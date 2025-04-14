using System.Collections.Concurrent;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Storage.DTO;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Kontur.TestCity.Worker.Handlers.Base;
using Microsoft.Extensions.Logging;

namespace Kontur.TestCity.Worker.Handlers;

public sealed class BuildCommitParentsHandler(SkbKonturGitLabClientProvider gitLabClientProvider, TestCityDatabase testCityDatabase) : TaskHandler<BuildCommitParentsTaskPayload>
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == BuildCommitParentsTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(BuildCommitParentsTaskPayload task, CancellationToken ct)
    {
        logger.LogInformation("Начало обработки BuildCommitParentsTask для проекта {ProjectId} и коммита {CommitSha}",
            task.ProjectId, task.CommitSha);

        (string, string CommitSha) processingKey = (task.ProjectId.ToString(), task.CommitSha);
        if (processedCommits.Contains(processingKey))
        {
            logger.LogInformation("Коммит {CommitSha} в проекте {ProjectId} уже был обработан, пропускаем",
                task.CommitSha, task.ProjectId);
            return;
        }

        var processLock = commitProcessLocks.GetOrAdd(processingKey, _ => new SemaphoreSlim(1, 1));
        await processLock.WaitAsync(ct);
        try
        {
            // Проверка наличия корневого коммита (с Depth = 0) в таблице CommitParents
            bool commitExists = await testCityDatabase.CommitParents.ExistsAsync(task.ProjectId, task.CommitSha, ct);
            if (commitExists)
            {
                logger.LogInformation("Коммит {CommitSha} в проекте {ProjectId} уже существует в таблице CommitParents, пропускаем",
                    task.CommitSha, task.ProjectId);
                processedCommits.Add(processingKey);
                return;
            }

            var client = gitLabClientProvider.GetExtendedClient();

            var commits = client.GetAllRepositoryCommitsAsync(task.ProjectId, x => x.ForReference(task.CommitSha), ct);

            var entries = await commits.Take(200).Select((x, i) => new CommitParentsEntry
            {
                ProjectId = task.ProjectId,
                CommitSha = task.CommitSha,
                ParentCommitSha = x.Id,
                Depth = i,
                AuthorName = x.AuthorName,
                AuthorEmail = x.AuthorEmail,
                MessagePreview = GetMessagePreview(x),
            }).ToListAsync(cancellationToken: ct);

            logger.LogInformation("Загружаем родительские коммиты для {CommitSha}", task.CommitSha);
            await testCityDatabase.CommitParents.InsertBatchAsync(entries, ct);

            processedCommits.Add(processingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке родительских коммитов для {CommitSha} в проекте {ProjectId}",
                task.CommitSha, task.ProjectId);
            throw;
        }
        finally
        {
            processLock.Release();
            logger.LogDebug("Снята блокировка для коммита {CommitSha} в проекте {ProjectId}",
                task.CommitSha, task.ProjectId);
        }
    }

    private static string? GetMessagePreview(GitLabCommit x)
    {
        var firstLine = x.Message?.Split('\n').FirstOrDefault();
        if (firstLine is null)
            return null;

        return firstLine[..Math.Min(100, firstLine.Length)];
    }

    private readonly ILogger logger = Log.GetLog<BuildCommitParentsHandler>();
    private static readonly ConcurrentBag<(string, string)> processedCommits = [];
    private static readonly ConcurrentDictionary<(string, string), SemaphoreSlim> commitProcessLocks = new();
}
