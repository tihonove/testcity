using System.IO.Compression;
using TestCity.Api.Models;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Logging;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;
using Microsoft.AspNetCore.Mvc;
using NGitLab;
using NGitLab.Models;
using System.Collections.Concurrent;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/gitlab")]
public class GitlabController(
    SkbKonturGitLabClientProvider gitLabClientProvider,
    GitLabProjectsService gitLabProjectsService,
    WorkerClient workerClient) : Controller
{
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
        log.LogInformation("Получен webhook от GitLab");

        GitLabJobEventInfo? jobEventInfo;
        try
        {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                log.LogWarning("Получен пустой запрос от GitLab webhook");
                return BadRequest("Тело запроса не может быть пустым");
            }

            log.LogDebug("Содержимое webhook: {RequestBody}", requestBody);

            jobEventInfo = System.Text.Json.JsonSerializer.Deserialize<GitLabJobEventInfo>(requestBody);
            if (jobEventInfo == null)

            {
                log.LogWarning("Не удалось десериализовать тело запроса в GitLabJobEventInfo");
                return BadRequest("Неверный формат данных");
            }

            log.LogInformation("Получен webhook от GitLab и десериализован: {ProjectId}, {JobRunId}", jobEventInfo.ProjectId, jobEventInfo.BuildId);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            log.LogError(jsonEx, "Ошибка при десериализации webhook данных от GitLab");
            return BadRequest("Неверный формат JSON");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Ошибка при чтении данных запроса GitLab webhook");
            return StatusCode(500);
        }

        try
        {
            if (await gitLabProjectsService.HasProject(jobEventInfo.ProjectId))
            {
                if (jobEventInfo.BuildStatus == "canceled" || jobEventInfo.BuildStatus == "failed" || jobEventInfo.BuildStatus == "success")
                {
                    await workerClient.Enqueue(new ProcessJobRunTaskPayload
                    {
                        ProjectId = jobEventInfo.ProjectId,
                        JobRunId = jobEventInfo.BuildId,
                    });
                }
                else if (jobEventInfo.BuildStatus == "running")
                {
                    await workerClient.Enqueue(new ProcessInProgressJobTaskPayload
                    {
                        ProjectId = jobEventInfo.ProjectId,
                        JobRunId = jobEventInfo.BuildId
                    });
                }
            }
            return Ok();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Ошибка при обработке webhook от GitLab");
            return StatusCode(500);
        }
    }

    [HttpGet("projects/{projectId}/access-check")]
    public async Task<IActionResult> CheckProjectAccess(long projectId)
    {
        log.LogInformation("Проверка доступа к проекту GitLab с ID: {ProjectId}", projectId);

        try
        {
            var projectInfo = await gitLabClient.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
            if (projectInfo == null)
            {
                log.LogWarning("Не удалось получить доступ к проекту с ID: {ProjectId} - проект не существует", projectId);
                return BadRequest("Проект не существует или нет доступа к нему. Проверьте ID проекта и права доступа.");
            }

            var clientEx = gitLabClientProvider.GetExtendedClient();

            try
            {
                var _ = await clientEx.GetAllProjectJobsAsync(projectId, perPage: 1).Take(1).ToArrayAsync();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Не удалось получить доступ к джобам проекта с ID: {ProjectId}", projectId);
                return BadRequest($"Проект '{projectInfo.Name}' найден, но нет доступа к его джобам. Проверьте права доступа.");
            }

            try
            {
                var response = await clientEx.GetRepositoryCommitsAsync(projectId, options => options.UseKeysetPagination(perPage: 1, orderBy: "created_at", sort: "desc"));
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Не удалось получить доступ к коммитам проекта с ID: {ProjectId}", projectId);
                return BadRequest($"Проект '{projectInfo.Name}' найден, но нет доступа к его коммитам. Проверьте права доступа.");
            }

            log.LogInformation("Успешная проверка доступа к проекту: {ProjectName} (ID: {ProjectId})", projectInfo.Name, projectId);
            return Ok($"Доступ к проекту '{projectInfo.Name}' и его данным подтвержден.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Ошибка при проверке доступа к проекту с ID: {ProjectId}", projectId);
            return BadRequest("Не удалось проверить доступ к проекту. Проверьте ID проекта и права доступа.");
        }
    }

    [HttpPost("projects/{projectId}/add")]
    public async Task<IActionResult> AddProject(long projectId, CancellationToken token)
    {
        log.LogInformation("Запрос на добавление проекта GitLab с ID: {ProjectId}", projectId);
        await gitLabProjectsService.AddProject(projectId);
        await ProcessProjectLastJobsAsync(projectId, token);
        return Ok($"Проект '{projectId}' успешно добавлен в систему.");
    }

    private async ValueTask ProcessProjectLastJobsAsync(long projectId, CancellationToken token)
    {
        var clientEx = gitLabClientProvider.GetExtendedClient();
        ConcurrentDictionary<(long, long), byte> processedCompletedJobSet = new();
        ConcurrentDictionary<(long, long), byte> processedRunningJobSet = new();
        log.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var jobs = await clientEx
            .GetAllProjectJobsAsync(projectId, Core.GitLab.Models.JobScope.Failed | Core.GitLab.Models.JobScope.Success | Core.GitLab.Models.JobScope.Running | Core.GitLab.Models.JobScope.Canceled, perPage: 100, token)
            .Take(600)
            .ToListAsync(token);
        jobs.Reverse();
        log.LogInformation("Take last {jobsLength} jobs", jobs.Count);
        var enqueuedCount = 0;
        foreach (var job in jobs)
        {
            if (job.Status == Core.GitLab.Models.JobStatus.Running)
            {
                if (processedRunningJobSet.ContainsKey((projectId, job.Id)))
                {
                    log.LogInformation("Skip job with id: {JobId}", job.Id);
                    continue;
                }

                try
                {
                    await workerClient.Enqueue(
                        new ProcessInProgressJobTaskPayload
                        {
                            ProjectId = projectId,
                            JobRunId = job.Id,
                        });
                    enqueuedCount++;
                    processedRunningJobSet.TryAdd((projectId, job.Id), 0);
                }
                catch (Exception exception)
                {
                    log.LogError(exception, "Failed to enqueue job {JobId}", job.Id);
                    continue;
                }
            }
            else
            {
                if (processedCompletedJobSet.ContainsKey((projectId, job.Id)))
                {
                    log.LogInformation("Skip job with id: {JobId}", job.Id);
                    continue;
                }

                try
                {
                    await workerClient.Enqueue(
                        new ProcessJobRunTaskPayload
                        {
                            ProjectId = projectId,
                            JobRunId = job.Id,
                        });
                    enqueuedCount++;
                    processedCompletedJobSet.TryAdd((projectId, job.Id), 0);
                }
                catch (Exception exception)
                {
                    log.LogError(exception, "Failed to enqueue job {JobId}", job.Id);
                    continue;
                }
            }
        }

        log.LogInformation("Enqueued {JobCount} jobs for {ProjectId}.", enqueuedCount, projectId);
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

    private readonly GitLabExtendedClient gitLabExtendedClient = gitLabClientProvider.GetExtendedClient();
    private readonly ILogger log = Log.GetLog<GitlabController>();
    private readonly IGitLabClient gitLabClient = gitLabClientProvider.GetClient();
}

