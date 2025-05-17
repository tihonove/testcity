using System.Collections.Concurrent;
using TestCity.Core.GitLab;
using TestCity.Core.GitLab.Models;
using TestCity.Core.JobProcessing;
using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;
using NGitLab;

namespace TestCity.Worker.Handlers;

public class ProcessInProgressJobTaskHandler(
    SkbKonturGitLabClientProvider gitLabClientProvider,
    TestCityDatabase testCityDatabase,
    ProjectJobTypesCache projectJobTypesCache,
    CommitParentsBuilderService commitParentsBuilder) : TaskHandler<ProcessInProgressJobTaskPayload>()
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == ProcessInProgressJobTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(ProcessInProgressJobTaskPayload task, CancellationToken ct)
    {
        logger.LogInformation("Обработка незавершенной задачи для проекта {ProjectId}, job run id: {JobRunId}",
            task.ProjectId, task.JobRunId);

        (string, long JobRunId) processingKey = (task.ProjectId.ToString(), task.JobRunId);
        if (processedJobs.Contains(processingKey))
        {
            logger.LogInformation("Задача {JobRunId} в проекте {ProjectId} уже была обработана, пропускаем", task.JobRunId, task.ProjectId);
            return;
        }

        var processLock = jobProcessLocks.GetOrAdd(processingKey, _ => new SemaphoreSlim(1, 1));
        await processLock.WaitAsync(ct);
        try
        {
            var job = await clientEx.GetJobAsync(task.ProjectId, task.JobRunId);
            var projectInfo = await client.Projects.GetByIdAsync(task.ProjectId, new(), ct);
            if (job.Commit?.Id is not null)
                await commitParentsBuilder.BuildCommitParent(task.ProjectId, job.Commit.Id, ct);

            if (job.Status != Core.GitLab.Models.JobStatus.Running)
            {
                logger.LogInformation("Задача {JobRunId} в проекте {ProjectId} завершилась со статусом {Status}. Пропускаем.", task.JobRunId, task.ProjectId, job.Status);
                processedJobs.Add(processingKey);
                return;
            }

            bool exists = await testCityDatabase.InProgressJobInfo.ExistsAsync(task.ProjectId.ToString(), task.JobRunId.ToString());

            if (!exists)
            {
                // Проверяем, существуют ли завершенные задачи такого типа для этого проекта
                string projectId = task.ProjectId.ToString();
                string jobType = job.Name;

                if (!await projectJobTypesCache.JobTypeExistsAsync(projectId, jobType, ct))
                {
                    logger.LogInformation("Тип задачи {JobType} не существует в списке завершенных задач для проекта {ProjectId}. Пропускаем.", jobType, projectId);
                }

                var refId = await client.BranchOrRef(task.ProjectId, job.Ref);
                var inProgressJobInfo = new InProgressJobInfo
                {
                    JobId = job.Name,
                    JobRunId = job.Id.ToString(),
                    JobUrl = job.WebUrl,
                    StartDateTime = job.StartedAt ?? DateTime.Now,
                    PipelineSource = job.Pipeline?.Source,
                    Triggered = job.User?.PublicEmail,
                    BranchName = refId,
                    CommitSha = job.Commit?.Id,
                    CommitMessage = job.Commit?.Message,
                    CommitAuthor = job.Commit?.AuthorName,
                    AgentName = job.Runner?.Name ?? job.Runner?.Description ?? $"agent_{job.Runner?.Id ?? 0}",
                    AgentOSName = job.RunnerManager?.Platform ?? "Unknown",
                    ProjectId = task.ProjectId.ToString(),
                    PipelineId = job.Pipeline?.Id.ToString(),
                };
                if (job.Commit?.Id is not null && job.Ref is not null)
                    inProgressJobInfo.ChangesSinceLastRun = await testCityDatabase.GetCommitChangesAsync(job.Commit.Id, job.Name, job.Ref, ct);
                await testCityDatabase.InProgressJobInfo.InsertAsync(inProgressJobInfo);
            }
        }
        finally
        {
            processLock.Release();
        }
    }

    private readonly ILogger logger = Log.GetLog<ProcessInProgressJobTaskHandler>();
    private readonly IGitLabClient client = gitLabClientProvider.GetClient();
    private readonly GitLabExtendedClient clientEx = gitLabClientProvider.GetExtendedClient();
    private static readonly ConcurrentBag<(string, long)> processedJobs = [];
    private static readonly ConcurrentDictionary<(string, long), SemaphoreSlim> jobProcessLocks = new();

}
