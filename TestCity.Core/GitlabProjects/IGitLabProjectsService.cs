namespace TestCity.Core.GitlabProjects;

public interface IGitLabProjectsService
{
    Task<List<GitLabProject>> GetAllProjects(CancellationToken cancellationToken = default);
    Task<bool> HasProject(long projectId, CancellationToken cancellationToken = default);
    Task<GitLabGroup?> GetRootGroup(string idOrTitle, CancellationToken cancellationToken = default);
    Task<List<GitLabGroupShortInfo>> GetRootGroups(CancellationToken cancellationToken = default);
}
