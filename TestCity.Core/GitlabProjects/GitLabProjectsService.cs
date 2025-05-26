using TestCity.Core.Logging;
using TestCity.Core.Storage;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading;
using TestCity.Core.GitLab;
using NGitLab;
using NGitLab.Models;

namespace TestCity.Core.GitlabProjects;

public class GitLabProjectsService : IDisposable
{
    public GitLabProjectsService(TestCityDatabase database, SkbKonturGitLabClientProvider gitLabClientProvider)
    {
        this.database = database;
        this.gitLabClientProvider = gitLabClientProvider;
        logger = this.LogForMe();
        _ = UpdateCacheAsync();
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
        return cachedRootGroups.ConvertAll(x => new GitLabGroupShortInfo { Id = x.Id, Title = x.Title, MergeRunsFromJobs = x.MergeRunsFromJobs, AvatarUrl = x.AvatarUrl });
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

    public async Task AddProject(long projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var gitLabClient = gitLabClientProvider.GetClient();
            var projectInfo = await gitLabClient.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
            if (projectInfo == null)
            {
                throw new Exception($"Не удалось получить доступ к проекту с ID: {projectId} - проект не существует");
            }

            var exists = await HasProject(projectId, cancellationToken);
            if (exists)
            {
                return;
            }

            var rootGroup = await BuildGroupHierarchyAsync(projectInfo.Namespace.Id, gitLabClient);
            GitLabGroup leafGroup = FindGroupById(rootGroup, projectInfo.Namespace.Id.ToString());

            leafGroup.Projects ??= [];
            leafGroup.Projects.Add(new GitLabProject
            {
                Id = projectId.ToString(),
                Title = projectInfo.Path,
                UseHooks = true
            });

            await SaveGitLabHierarchy([rootGroup], cancellationToken);

            logger.LogInformation("Успешно добавлен проект: {ProjectName} (ID: {ProjectId}) в иерархию групп", projectInfo.Name, projectId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при добавлении проекта с ID: {projectId}", ex);
        }
    }

    private async Task<GitLabGroup> BuildGroupHierarchyAsync(long namespaceId, IGitLabClient gitLabClient)
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
            var parentGroup = await BuildGroupHierarchyAsync(currentGroupInfo.ParentId.Value, gitLabClient);
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

    public void Dispose()
    {
        refreshTimer?.Dispose();
        cacheInitializationSemaphore?.Dispose();
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
                    logger.LogError(ex, "Ошибка при обновлении кэша проектов GitLab");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при вызове Dispose
        }
    }

    protected virtual async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
    {
        await cacheInitializationSemaphore.WaitAsync(cancellationToken);
        try
        {
            cacheInitializationInProgress = true;
            logger.LogInformation("Обновление кэша GitLab проектов...");

            var gitLabClient = gitLabClientProvider.GetClient();
            var rootGroups = await database.GitLabEntities.GetAllEntitiesAsync(cancellationToken).ToGitLabGroups(cancellationToken);
            await rootGroups.TraverseRecursiveAsync(async g =>
            {
                try
                {
                    if (g.Id == "0")
                        return;
                    var groupInfo = await gitLabClient.Groups.GetByIdAsync(long.Parse(g.Id));
                    g.AvatarUrl = groupInfo?.AvatarUrl;

                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Не удалось получить информацию о группе с ID {GroupId} из GitLab", g.Id);
                }
            }, async p =>
            {
                try
                {
                    if (p.Id == "0")
                        return;
                    var projectInfo = await gitLabClient.Projects.GetByIdAsync(long.Parse(p.Id), new SingleProjectQuery());
                    p.AvatarUrl = projectInfo?.AvatarUrl;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Не удалось получить информацию о проекте с ID {ProjectId} из GitLab", p.Id);
                }
            }, cancellationToken: cancellationToken);
            lock (cacheLock)
            {
                cachedRootGroups = rootGroups;
                isCacheInitialized = true;
            }

            logger.LogInformation("Кэш GitLab проектов обновлен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "");
            throw;
        }
        finally
        {
            cacheInitializationInProgress = false;
            cacheInitializationSemaphore.Release();
        }
    }

    protected async Task EnsureCacheInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (isCacheInitialized)
            return;
            
        if (cacheInitializationInProgress)
        {
            // Кэш уже инициализируется в другом потоке, ожидаем завершения
            logger.LogInformation("Ожидание инициализации кэша GitLab проектов...");
            await cacheInitializationSemaphore.WaitAsync(cancellationToken);
            cacheInitializationSemaphore.Release(); // Освобождаем семафор для других ожидающих потоков
        }
        else if (!isCacheInitialized)
        {
            // В редком случае, когда флаг cacheInitializationInProgress не установлен, но кэш не инициализирован
            // Это может произойти при первом запуске или при сбросе кэша
            logger.LogInformation("Кэш не инициализирован и не инициализируется, начинаем инициализацию");
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
    private readonly SkbKonturGitLabClientProvider gitLabClientProvider;
    private readonly ILogger logger;
    private readonly PeriodicTimer refreshTimer;
    private readonly Lock cacheLock = new();
    private List<GitLabGroup> cachedRootGroups = [];
    private bool isCacheInitialized;
    private readonly SemaphoreSlim cacheInitializationSemaphore = new(1, 1);
    private bool cacheInitializationInProgress;
}
