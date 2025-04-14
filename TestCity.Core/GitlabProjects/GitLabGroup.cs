namespace Kontur.TestCity.Core.GitlabProjects;

public class GitLabGroup
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    public List<GitLabProject> Projects { get; set; } = new List<GitLabProject>();

    public List<GitLabGroup> Groups { get; set; } = new List<GitLabGroup>();
}
