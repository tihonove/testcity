using System.IO.Compression;
using Kontur.TestCity.Core;
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
    public async Task<IActionResult> WebhookHandler([FromBody] GitLabJobEventInfo jobEventInfo)
    {
        try
        {
            if (hooksBasedProjectIds.Contains(jobEventInfo.ProjectId))
            {
                if (jobEventInfo.BuildStatus == GitLabJobEventBuildStatus.Failed || jobEventInfo.BuildStatus == GitLabJobEventBuildStatus.Success)
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
