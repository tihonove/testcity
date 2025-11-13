namespace TestCity.Core.GitlabProjects;

public class GitLabEntity
{
    public required string Id { get; set; }
    public required string Title { get; set; }    
    public string? AvatarUrl { get; set; }
}
