namespace Kontur.TestAnalytics.Core.Models;

public class GitLabGroup
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    public List<GitLabProject> Projects { get; set; } = new List<GitLabProject>();

    public List<GitLabGroup> Groups { get; set; } = new List<GitLabGroup>();
}

public class GitLabProject
{
    public required string Id { get; set; }

    public required string Title { get; set; }
}
