using TestCity.Core.Logging;
using TestCity.Core.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace TestCity.Core.GitlabProjects;

public class GitLabProjectsService : IDisposable
{
    public GitLabProjectsService(TestCityDatabase database)
    {
        this.database = database;
        logger = this.LogForMe();

        // Инициализируем кэш при создании экземпляра
        _ = UpdateCacheAsync();

        // Запускаем фоновый таймер для обновления кэша
        refreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _ = StartRefreshLoop();
    }

    public async Task<List<GitLabProject>> GetAllProjects(CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return GetAllProjectsRecursive(cachedRootGroups).ToList();
    }

    public async Task<List<GitLabGroupShortInfo>> GetRootGroupsInfo(CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return cachedRootGroups.ConvertAll(x => new GitLabGroupShortInfo { Id = x.Id, Title = x.Title, MergeRunsFromJobs = x.MergeRunsFromJobs });
    }

    public async Task<GitLabGroup?> GetGroup(string idOrTitle, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return cachedRootGroups.FirstOrDefault(g => g.Id == idOrTitle || g.Title.Equals(idOrTitle, StringComparison.OrdinalIgnoreCase));
    }

    public async IAsyncEnumerable<long> EnumerateAllProjectsIds([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var allProjects = await GetAllProjects(cancellationToken);
        foreach (var project in allProjects)
        {
            yield return long.Parse(project.Id);
        }
    }

    public async Task<bool> HasProject(long projectId, CancellationToken cancellationToken = default)
    {
        return await EnumerateAllProjectsIds(cancellationToken).ContainsAsync(projectId, cancellationToken);
    }

    public async Task SaveGitLabHierarchy(List<GitLabGroup> rootGroups, CancellationToken cancellationToken = default)
    {
        var allEntities = rootGroups.ToGitLabEntityRecords(null);
        await database.GitLabEntities.UpsertEntitiesAsync(allEntities, cancellationToken);
        await UpdateCacheAsync(cancellationToken);
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task StartRefreshLoop()
    {
        try
        {
            while (await refreshTimer.WaitForNextTickAsync())
            {
                try
                {
                    await UpdateCacheAsync();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Ошибка при обновлении кэша проектов GitLab");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при вызове Dispose
        }
    }

    private async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
    {
        logger?.LogDebug("Обновление кэша GitLab проектов...");

        try
        {
            var rootGroups = await database.GitLabEntities.GetAllEntitiesAsync(cancellationToken).ToGitLabGroups(cancellationToken);
            lock (cacheLock)
            {
                cachedRootGroups = rootGroups;
                isCacheInitialized = true;
            }

            logger?.LogDebug("Кэш GitLab проектов обновлен");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Не удалось обновить кэш GitLab проектов");
            throw;
        }
    }

    private async Task EnsureCacheInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (!isCacheInitialized)
        {
            await UpdateCacheAsync(cancellationToken);
        }
    }

    private static IEnumerable<GitLabProject> GetAllProjectsRecursive(IEnumerable<GitLabGroup> groups)
    {
        foreach (var group in groups)
        {
            foreach (var project in group.Projects ?? [])
            {
                yield return project;
            }
            foreach (var project in GetAllProjectsRecursive(group.Groups ?? []))
            {
                yield return project;
            }
        }
    }

    private readonly TestCityDatabase database;
    private readonly ILogger logger;
    private readonly PeriodicTimer refreshTimer;
    private readonly Lock cacheLock = new();
    private List<GitLabGroup> cachedRootGroups = [];
    private bool isCacheInitialized;
}
