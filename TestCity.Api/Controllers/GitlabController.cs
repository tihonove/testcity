using System.IO.Compression;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.AspNetCore.Mvc;
using NGitLab;

namespace Kontur.TestCity.Api.Controllers;

[ApiController]
[Route("api/gitlab")]
public class GitlabController(SkbKonturGitLabClientProvider gitLabClientProvider, WorkerClient workerClient, ILogger<GitlabController> logger) : Controller
{
    private readonly IGitLabClient gitLabClient = gitLabClientProvider.GetClient();
    private readonly HashSet<long> hooksBasedProjectIds = GitLabProjectsService.GetAllProjects().Select(x => long.Parse(x.Id)).ToHashSet();

    [HttpGet("{projectId}/jobs/{jobId}/codequality")]
    public IActionResult Get(long projectId, long jobId)
    {
        var art = gitLabClient.GetJobs(projectId).GetJobArtifacts(jobId);
        if (art == null)
        {
            return NotFound();
        }

        var codequality = TryExtractTestRunsFromGitlabArtifact(art);
        if (codequality == null)
        {
            return NotFound();
        }

        return Ok(codequality);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> WebhookHandler()
    {
        logger.LogInformation("Получен webhook от GitLab");

        GitLabJobEventInfo? jobEventInfo;
        try
        {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                logger.LogWarning("Получен пустой запрос от GitLab webhook");
                return BadRequest("Тело запроса не может быть пустым");
            }

            logger.LogDebug("Содержимое webhook: {RequestBody}", requestBody);

            jobEventInfo = System.Text.Json.JsonSerializer.Deserialize<GitLabJobEventInfo>(requestBody);
            if (jobEventInfo == null)
            {
                logger.LogWarning("Не удалось десериализовать тело запроса в GitLabJobEventInfo");
                return BadRequest("Неверный формат данных");
            }

            logger.LogInformation("Получен webhook от GitLab и десериализован: {ProjectId}, {JobRunId}", jobEventInfo.ProjectId, jobEventInfo.BuildId);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Ошибка при десериализации webhook данных от GitLab");
            return BadRequest("Неверный формат JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при чтении данных запроса GitLab webhook");
            return StatusCode(500);
        }

        try
        {
            if (hooksBasedProjectIds.Contains(jobEventInfo.ProjectId))
            {
                if (jobEventInfo.BuildStatus == "failed" || jobEventInfo.BuildStatus == "success")
                    await workerClient.Enqueue(new ProcessJobRunTaskPayload
                    {
                        ProjectId = jobEventInfo.ProjectId,
                        JobRunId = jobEventInfo.BuildId,
                    });
            }
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке webhook от GitLab");
            return StatusCode(500);
        }
    }

    [HttpGet("{projectId}/pipelines/{pipelineId}/manual-jobs")]
    public ManualJobRunInfo[] GetManualJobInfos(long projectId, long pipelineId)
    {
        var jobs = gitLabClient.GetPipelines(projectId).GetJobs(pipelineId);
        var jobs2 = gitLabClient.GetPipelines(projectId).GetBridgesAsync(new NGitLab.Models.PipelineBridgeQuery { PipelineId = pipelineId }).ToArray();
        if (jobs == null && jobs2 == null)
        {
            return Array.Empty<ManualJobRunInfo>();
        }

        var deployJobs = jobs?.Where(x => x.Name.Contains("deploy", StringComparison.InvariantCultureIgnoreCase)).ToArray() ?? Array.Empty<NGitLab.Models.Job>();
        var deployJobs2 = jobs2.Where(x => x.Name.Contains("deploy", StringComparison.InvariantCultureIgnoreCase)).ToArray() ?? Array.Empty<NGitLab.Models.Bridge>();
        return deployJobs.Select(j => new ManualJobRunInfo()
        {
            JobId = j.Name,
            JobRunId = j.Id.ToString(),
            Status =
                j.Status == JobStatus.Manual ? ManualJobRunStatus.Manual :
                j.Status == JobStatus.Success ? ManualJobRunStatus.Susccess :
                ManualJobRunStatus.Failed,
        }).Concat(deployJobs2.Select(j => new ManualJobRunInfo()
        {
            JobId = j.Name,
            JobRunId = j.Id.ToString(),
            Status =
                j.Status == JobStatus.Manual ? ManualJobRunStatus.Manual :
                j.Status == JobStatus.Success ? ManualJobRunStatus.Susccess :
                ManualJobRunStatus.Failed,
        })).ToArray();
    }

    private static string? TryExtractTestRunsFromGitlabArtifact(byte[] artifactContent)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);

        try
        {
            using (var zipStream = new MemoryStream(artifactContent))
            using (var archive = new ZipArchive(zipStream))
            {
                archive.ExtractToDirectory(tempPath);
            }

            return Directory.EnumerateFiles(tempPath, "*.json", SearchOption.AllDirectories)
                .Select(System.IO.File.ReadAllText)
                .FirstOrDefault(file => file.Contains("\"fingerprint\"") &&
                                        file.Contains("\"check_name\"") &&
                                        file.Contains("\"severity\""));
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }
}
