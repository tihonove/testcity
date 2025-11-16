using TestCity.Core.GitlabProjects;

namespace TestCity.UnitTests.AccessChecking;

public class InMemoryGitLabProjectsService : IGitLabProjectsService
{
    public InMemoryGitLabProjectsService()
    {
    }

    public InMemoryGitLabProjectsService(List<GitLabGroup> rootGroups)
    {
        this.rootGroups = rootGroups;
    }

    public Task<List<GitLabProject>> GetAllProjects(CancellationToken cancellationToken = default)
    {
        var projects = GetAllProjectsRecursive(rootGroups).ToList();
        return Task.FromResult(projects);
    }

    public Task<GitLabGroup?> GetRootGroup(string idOrTitle, CancellationToken cancellationToken = default)
    {
        var group = rootGroups.FirstOrDefault(g =>
            g.Id == idOrTitle ||
            g.Title.Equals(idOrTitle, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(group);
    }

    public Task<List<GitLabGroupShortInfo>> GetRootGroups(CancellationToken cancellationToken = default)
    {
        var shortInfos = rootGroups.ConvertAll(x => x.CloneShortInfo());
        return Task.FromResult(shortInfos);
    }

    public Task<bool> HasProject(long projectId, CancellationToken cancellationToken = default)
    {
        var projectIdStr = projectId.ToString();
        var hasProject = GetAllProjectsRecursive(rootGroups).Any(p => p.Id == projectIdStr);
        return Task.FromResult(hasProject);
    }

    public void AddRootGroup(GitLabGroup group)
    {
        rootGroups.Add(group);
    }

    public void ClearRootGroups()
    {
        rootGroups.Clear();
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

    private readonly List<GitLabGroup> rootGroups = [];
}
