using System.IO.Compression;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NGitLab;

namespace Kontur.TestCity.Api.Controllers;

[ApiController]
[Route("api/gitlab")]
public class GitlabController : Controller
{
    private readonly IGitLabClient gitLabClient;
    private readonly List<GitLabProject> projects;
    private readonly ILogger<GitlabController> logger;

    public GitlabController(SkbKonturGitLabClientProvider gitLabClientProvider, ILogger<GitlabController> logger)
    {
        gitLabClient = gitLabClientProvider.GetClient();
        this.projects = GitLabProjectsService.GetAllProjects();
        this.logger = logger;
    }

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
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            logger.LogInformation("Получен webhook от GitLab: {WebhookBody}", body);
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
