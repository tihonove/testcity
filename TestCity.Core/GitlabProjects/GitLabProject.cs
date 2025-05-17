namespace TestCity.Core.GitlabProjects;

public class GitLabProject
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public bool UseHooks { get; set; }
}
