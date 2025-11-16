using System.Reflection.Metadata;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects.AccessChecking;

namespace TestCity.Core.GitlabProjects;

public class GitLabPathResolver(IGitLabProjectsService gitLabProjectsService, IGitLabEntityAccessContext accessContext)
{
    public async Task<List<GitLabGroupShortInfo>> GetRootGroupsInfo()
    {
        var accessEntries = await accessContext.ListAccessEntries();
        var accessibleRoots = accessEntries.Select(x => x.PathSlug[0]).ToHashSet();
        var allGroups = await gitLabProjectsService.GetRootGroups();
        return allGroups.Where(g => (g.IsPublic ?? false) || accessibleRoots.Contains(g.Title)).ToList();
    }

    public async Task<ResolveGroupOrProjectPathResult> ResolveGroupOrProjectPath(string[] pathSlug)
    {
        var accessEntries = (await accessContext.ListAccessEntries()).ToDictionary(x => string.Join("/", x.PathSlug), x => x);
        var resolvedPathSlug = new List<GitLabEntity>();
        GitLabEntity? currentEntity = null;
        bool currentEntityIsPublic = false;
        AccessControlEntry? nearestAccessEntry = accessEntries.GetValueOrDefault("");

        foreach (var entityPath in pathSlug)
        {
            if (currentEntity == null)
            {
                currentEntity = await gitLabProjectsService.GetRootGroup(entityPath);
            }
            else
            {
                var currentEntityChildren = currentEntity is GitLabGroup group ? group.Groups.AsEnumerable<GitLabEntity>().Concat(group.Projects) : [];
                currentEntity = currentEntityChildren.FirstOrDefault(g => g.Id == entityPath || g.Title == entityPath);
            }
            if (currentEntity == null)
            {
                throw new Exception($"Группа с идентификатором или названием '{entityPath}' не найдена");
            }
            resolvedPathSlug.Add(currentEntity);
            currentEntityIsPublic = currentEntity.IsPublic ?? currentEntityIsPublic;
            nearestAccessEntry = accessEntries.GetValueOrDefault(string.Join("/", resolvedPathSlug.Select(x => x.Title))) ?? nearestAccessEntry;
        }

        if (!currentEntityIsPublic && (nearestAccessEntry?.HasAccess == false))
        {
            throw new AccessToEntityForbiddenException($"Доступ к группе или проекту '{currentEntity?.Title ?? "null"}' запрещён");
        }

        var currentPath = resolvedPathSlug.Select(x => x.Title).ToList();
        return new ResolveGroupOrProjectPathResult(
            resolvedPathSlug.Select(x => x.CloneEntiry()).ToArray(),
            FilterOutInaccessibleChildren(
                accessEntries,
                currentEntity ?? throw new Exception($"Группа с идентификатором или названием '{pathSlug.Last()}' не найдена"),
                currentEntityIsPublic,
                nearestAccessEntry,
                currentPath
            )
        );
    }

    private T FilterOutInaccessibleChildren<T>(
        Dictionary<string, AccessControlEntry> accessEntries,
        T currentEntity,
        bool currentEntityIsPublic,
        AccessControlEntry? nearestAccessEntry,
        List<string> currentPath)
        where T : GitLabEntity
    {
        if (currentEntity is GitLabProject)
        {
            return currentEntity;
        }

        if (currentEntity is GitLabGroup currentGroup)
        {
            var filteredGroups = new List<GitLabGroup>();
            var filteredProjects = new List<GitLabProject>();

            foreach (var group in currentGroup.Groups ?? [])
            {
                var childPath = currentPath.Concat([group.Title]).ToList();
                var pathSlugKey = string.Join("/", childPath);
                var accessEntry = accessEntries.GetValueOrDefault(pathSlugKey) ?? nearestAccessEntry;
                var isGroupPublic = group.IsPublic ?? currentEntityIsPublic;
                if ((group.IsPublic ?? false) || (accessEntry?.HasAccess != false))
                {
                    filteredGroups.Add(FilterOutInaccessibleChildren(accessEntries, group, isGroupPublic, accessEntry, childPath));
                }
            }

            foreach (var project in currentGroup.Projects ?? [])
            {
                var childPath = currentPath.Concat([project.Title]).ToList();
                var pathSlugKey = string.Join("/", childPath);
                var accessEntry = accessEntries.GetValueOrDefault(pathSlugKey) ?? nearestAccessEntry;
                if ((project.IsPublic ?? false) || (accessEntry?.HasAccess != false))
                {
                    filteredProjects.Add(project);
                }
            }
            return new GitLabGroup
            {
                Id = currentGroup.Id,
                Title = currentGroup.Title,
                AvatarUrl = currentGroup.AvatarUrl,
                Groups = filteredGroups,
                Projects = filteredProjects
            } as T ?? throw new Exception("Unexpected error during filtering inaccessible children");
        }
        throw new Exception("Unexpected entity type during filtering inaccessible children");
    }

    public async Task<GitLabProject[]> ResolveProjects(string[] groupIdOrTitles)
    {
        GitLabGroup? currentGroup = null;
        for (int i = 0; i < groupIdOrTitles.Length; i++)
        {
            var idOrTitle = groupIdOrTitles[i];
            if (currentGroup == null)
            {
                currentGroup = await gitLabProjectsService.GetRootGroup(idOrTitle);
            }
            else
            {
                var nextGroup = currentGroup.Groups.FirstOrDefault(g => g.Id.ToString() == idOrTitle || g.Title == idOrTitle);
                if (nextGroup == null && i == groupIdOrTitles.Length - 1)
                {
                    var project = currentGroup.Projects.FirstOrDefault(p => p.Id == idOrTitle || p.Title == idOrTitle);
                    if (project != null)
                    {
                        return [project];
                    }
                }
                currentGroup = nextGroup;
            }

            if (currentGroup == null)
                throw new Exception($"Группа с идентификатором или названием '{idOrTitle}' не найдена");
        }

        if (currentGroup == null)
            return [];

        return GetAllProjectsRecursive(currentGroup).ToArray();
    }

    private IEnumerable<GitLabProject> GetAllProjectsRecursive(GitLabGroup group)
    {
        foreach (var project in group.Projects ?? [])
        {
            yield return project;
        }
        foreach (var childGroup in group.Groups ?? [])
        {
            foreach (var project in GetAllProjectsRecursive(childGroup))
            {
                yield return project;
            }
        }
    }
}

public record ResolveGroupOrProjectPathResult(GitLabEntity[] PathSlug, GitLabEntity ResolvedEntity);

public class AccessToEntityForbiddenException : Exception
{
    public AccessToEntityForbiddenException(string message) : base(message)
    {
    }
}
