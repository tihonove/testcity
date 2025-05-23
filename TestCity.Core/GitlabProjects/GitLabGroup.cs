namespace TestCity.Core.GitlabProjects;

public class GitLabGroup : GitLabGroupShortInfo
{
    public List<GitLabProject> Projects { get; set; } = [];
    public List<GitLabGroup> Groups { get; set; } = [];
}
