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
        log.LogInformation("Received webhook from GitLab");

        GitLabJobEventInfo? jobEventInfo;
        try
        {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                log.LogWarning("Received empty request from GitLab webhook");
                return BadRequest("Request body cannot be empty");
            }

            log.LogDebug("Webhook content: {RequestBody}", requestBody);

            jobEventInfo = System.Text.Json.JsonSerializer.Deserialize<GitLabJobEventInfo>(requestBody);
            if (jobEventInfo == null)

            {
                log.LogWarning("Failed to deserialize request body to GitLabJobEventInfo");
                return BadRequest("Invalid data format");
            }

            log.LogInformation("Received webhook from GitLab and deserialized: {ProjectId}, {JobRunId}", jobEventInfo.ProjectId, jobEventInfo.BuildId);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            log.LogError(jsonEx, "Error deserializing webhook data from GitLab");
            return BadRequest("Invalid JSON format");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error reading GitLab webhook request data");
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
            log.LogError(ex, "Error processing webhook from GitLab");
            return StatusCode(500);
        }
    }

    [HttpGet("projects/{projectId}/access-check")]
    public async Task<IActionResult> CheckProjectAccess(long projectId)
    {
        log.LogInformation("Checking access to GitLab project with ID: {ProjectId}", projectId);

        try
        {
            var projectInfo = await gitLabClient.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
            if (projectInfo == null)
            {
                log.LogWarning("Failed to access project with ID: {ProjectId} - project does not exist", projectId);
                return BadRequest("Project does not exist or access is denied. Check project ID and access permissions.");
            }

            var clientEx = gitLabClientProvider.GetExtendedClient();

            try
            {
                var _ = await clientEx.GetAllProjectJobsAsync(projectId, perPage: 1).Take(1).ToArrayAsync();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to access jobs in project with ID: {ProjectId}", projectId);
                return BadRequest($"Project '{projectInfo.Name}' was found, but access to its jobs is denied. Check access permissions.");
            }

            try
            {
                var response = await clientEx.GetRepositoryCommitsAsync(projectId, options => options.UseKeysetPagination(perPage: 1, orderBy: "created_at", sort: "desc"));
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to access commits in project with ID: {ProjectId}", projectId);
                return BadRequest($"Project '{projectInfo.Name}' was found, but access to its commits is denied. Check access permissions.");
            }

            log.LogInformation("Successfully verified access to project: {ProjectName} (ID: {ProjectId})", projectInfo.Name, projectId);
            return Ok($"Access to project '{projectInfo.Name}' and its data confirmed.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error while checking access to project with ID: {ProjectId}", projectId);
            return BadRequest("Failed to verify project access. Check project ID and access permissions.");
        }
    }

    [HttpPost("projects/{projectId}/add")]
    public async Task<IActionResult> AddProject(long projectId, CancellationToken token)
    {
        log.LogInformation("Request to add GitLab project with ID: {ProjectId}", projectId);
        await gitLabProjectsService.AddProject(projectId);
        await ProcessProjectLastJobsAsync(projectId, token);
        return Ok($"Project '{projectId}' successfully added to the system.");
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

