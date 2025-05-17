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

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/gitlab")]
public class GitlabController(SkbKonturGitLabClientProvider gitLabClientProvider, GitLabProjectsService gitLabProjectsService, WorkerClient workerClient) : Controller
{
    private readonly ILogger logger = Log.GetLog<GitlabController>();
    private readonly IGitLabClient gitLabClient = gitLabClientProvider.GetClient();

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
            logger.LogError(ex, "Ошибка при обработке webhook от GitLab");
            return StatusCode(500);
        }
    }

    [HttpGet("projects/{projectId}/access-check")]
    public async Task<IActionResult> CheckProjectAccess(long projectId)
    {
        logger.LogInformation("Проверка доступа к проекту GitLab с ID: {ProjectId}", projectId);

        try
        {
            var projectInfo = await gitLabClient.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
            if (projectInfo == null)
            {
                logger.LogWarning("Не удалось получить доступ к проекту с ID: {ProjectId} - проект не существует", projectId);
                return BadRequest("Проект не существует или нет доступа к нему. Проверьте ID проекта и права доступа.");
            }

            var clientEx = gitLabClientProvider.GetExtendedClient();

            try
            {
                var _ = await clientEx.GetAllProjectJobsAsync(projectId, perPage: 1).Take(1).ToArrayAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось получить доступ к джобам проекта с ID: {ProjectId}", projectId);
                return BadRequest($"Проект '{projectInfo.Name}' найден, но нет доступа к его джобам. Проверьте права доступа.");
            }

            try
            {
                var response = await clientEx.GetRepositoryCommitsAsync(projectId, options => options.UseKeysetPagination(perPage: 1, orderBy: "created_at", sort: "desc"));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось получить доступ к коммитам проекта с ID: {ProjectId}", projectId);
                return BadRequest($"Проект '{projectInfo.Name}' найден, но нет доступа к его коммитам. Проверьте права доступа.");
            }

            logger.LogInformation("Успешная проверка доступа к проекту: {ProjectName} (ID: {ProjectId})", projectInfo.Name, projectId);
            return Ok($"Доступ к проекту '{projectInfo.Name}' и его данным подтвержден.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке доступа к проекту с ID: {ProjectId}", projectId);
            return BadRequest("Не удалось проверить доступ к проекту. Проверьте ID проекта и права доступа.");
        }
    }

    [HttpPost("projects/{projectId}/add")]
    public async Task<IActionResult> AddProject(long projectId)
    {
        logger.LogInformation("Запрос на добавление проекта GitLab с ID: {ProjectId}", projectId);

        try
        {
            // Проверяем существование проекта и доступ к нему
            var projectInfo = await gitLabClient.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
            if (projectInfo == null)
            {
                logger.LogWarning("Не удалось получить доступ к проекту с ID: {ProjectId} - проект не существует", projectId);
                return BadRequest("Проект не существует или нет доступа к нему. Проверьте ID проекта и права доступа.");
            }

            // Проверяем, существует ли проект уже в системе
            var exists = await gitLabProjectsService.HasProject(projectId);
            if (exists)
            {
                logger.LogInformation("Проект с ID: {ProjectId} уже существует в системе", projectId);
                return Ok($"Проект '{projectInfo.Name}' уже добавлен в систему.");
            }

            var rootGroup = await BuildGroupHierarchyAsync(projectInfo.Namespace.Id);
            GitLabGroup leafGroup = FindGroupById(rootGroup, projectInfo.Namespace.Id.ToString());

            leafGroup.Projects ??= [];
            leafGroup.Projects.Add(new GitLabProject
            {
                Id = projectId.ToString(),
                Title = projectInfo.Path,
                UseHooks = true
            });

            // Сохраняем иерархию в системе
            await gitLabProjectsService.SaveGitLabHierarchy([rootGroup]);

            logger.LogInformation("Успешно добавлен проект: {ProjectName} (ID: {ProjectId}) в иерархию групп", projectInfo.Name, projectId);
            return Ok($"Проект '{projectInfo.Name}' успешно добавлен в систему.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении проекта с ID: {ProjectId}", projectId);
            return StatusCode(500, "Произошла ошибка при добавлении проекта. Пожалуйста, попробуйте позже.");
        }
    }

    private async Task<GitLabGroup> BuildGroupHierarchyAsync(long namespaceId)
    {
        var currentGroupInfo = await gitLabClient.Groups.GetByIdAsync(namespaceId);
        var currentGroup = new GitLabGroup
        {
            Id = currentGroupInfo.Id.ToString(),
            Title = currentGroupInfo.Path,
            Groups = []
        };

        if (currentGroupInfo.ParentId.HasValue)
        {
            var parentGroup = await BuildGroupHierarchyAsync(currentGroupInfo.ParentId.Value);
            var targetParentGroup = FindGroupById(parentGroup, currentGroupInfo.ParentId.Value.ToString());
            targetParentGroup.Groups ??= [];
            targetParentGroup.Groups.Add(currentGroup);
            return parentGroup;
        }

        return currentGroup;
    }

    private static GitLabGroup FindGroupById(GitLabGroup group, string targetId)
    {
        if (group.Id == targetId)
            return group;

        if (group.Groups != null)
        {
            foreach (var childGroup in group.Groups)
            {
                var result = FindGroupById(childGroup, targetId);
                if (result != null)
                    return result;
            }
        }

        return group;
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
}

