namespace TestCity.Core.GitlabProjects;

public class GitLabGroupShortInfo
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? MergeRunsFromJobs { get; set; }
}
