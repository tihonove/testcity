using System.Collections.Concurrent;
using TestCity.Core.GitLab;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using Microsoft.Extensions.Logging;

namespace TestCity.Core.JobProcessing;

public sealed class CommitParentsBuilderService(SkbKonturGitLabClientProvider gitLabClientProvider, TestCityDatabase testCityDatabase)
{
    public async Task BuildCommitParent(long projectId, string commitSha, CancellationToken ct)
    {
        logger.LogInformation("Начало обработки BuildCommitParentsTask для проекта {ProjectId} и коммита {CommitSha}",
            projectId, commitSha);

        (string, string CommitSha) processingKey = (projectId.ToString(), commitSha);
        if (processedCommits.Contains(processingKey))
        {
            logger.LogInformation("Коммит {CommitSha} в проекте {ProjectId} уже был обработан, пропускаем",
                commitSha, projectId);
            return;
        }

        var processLock = commitProcessLocks.GetOrAdd(processingKey, _ => new SemaphoreSlim(1, 1));
        await processLock.WaitAsync(ct);
        try
        {
            // Проверка наличия корневого коммита (с Depth = 0) в таблице CommitParents
            bool commitExists = await testCityDatabase.CommitParents.ExistsAsync(projectId, commitSha, ct);
            if (commitExists)
            {
                logger.LogInformation("Коммит {CommitSha} в проекте {ProjectId} уже существует в таблице CommitParents, пропускаем",
                    commitSha, projectId);
                processedCommits.Add(processingKey);
                return;
            }

            var client = gitLabClientProvider.GetExtendedClient();
            var commits = await client.GetAllRepositoryCommitsAsync(projectId, x => x.ForReference(commitSha), ct)
                .Take(200)
                .ToListAsync(cancellationToken: ct);

            if (commits.Count == 0)
            {
                logger.LogWarning("Для коммита {CommitSha} в проекте {ProjectId} не найдены родительские коммиты",
                    commitSha, projectId);
                return;
            }

            var mainCommitShas = new HashSet<string>();
            var sideCommitShas = new HashSet<string>();
            var entries = new List<CommitParentsEntry>();
            var depth = 0;
            foreach (var commit in commits)
            {
                var branchType = sideCommitShas.Contains(commit.Id) && !mainCommitShas.Contains(commit.Id) ? BranchType.Side : BranchType.Main;
                entries.Add(new CommitParentsEntry
                {
                    ProjectId = projectId,
                    CommitSha = commitSha,
                    ParentCommitSha = commit.Id,
                    Depth = depth,
                    AuthorName = commit.AuthorName,
                    AuthorEmail = commit.AuthorEmail,
                    MessagePreview = GetMessagePreview(commit),
                    BranchType = branchType
                });
                if (branchType == BranchType.Main)
                {
                    mainCommitShas.Add(commit.ParentIds.FirstOrDefault() ?? "");
                    sideCommitShas.Remove(commit.ParentIds.FirstOrDefault() ?? "");
                    foreach (var sideCommit in commit.ParentIds.Skip(1))
                        sideCommitShas.Add(sideCommit);
                }
                else
                {
                    foreach (var sideCommit in commit.ParentIds)
                        sideCommitShas.Add(sideCommit);
                }
                depth++;
            }

            logger.LogInformation("Загружаем родительские коммиты для {CommitSha}, найдено {Count} коммитов",
                commitSha, entries.Count);
            await testCityDatabase.CommitParents.InsertBatchAsync(entries, ct);

            processedCommits.Add(processingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке родительских коммитов для {CommitSha} в проекте {ProjectId}",
                commitSha, projectId);
            throw;
        }
        finally
        {
            processLock.Release();
            logger.LogDebug("Снята блокировка для коммита {CommitSha} в проекте {ProjectId}",
                commitSha, projectId);
        }
    }

    private static string? GetMessagePreview(GitLabCommit x)
    {
        var firstLine = x.Message?.Split('\n').FirstOrDefault();
        if (firstLine is null)
            return null;

        return firstLine[..Math.Min(100, firstLine.Length)];
    }

    private readonly ILogger logger = Log.GetLog<CommitParentsBuilderService>();
    private static readonly ConcurrentBag<(string, string)> processedCommits = [];
    private static readonly ConcurrentDictionary<(string, string), SemaphoreSlim> commitProcessLocks = new();
}
