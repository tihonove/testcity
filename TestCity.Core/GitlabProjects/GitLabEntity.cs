namespace TestCity.Core.GitlabProjects;

public class GitLabEntity
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? IsPublic { get; set; }

    public GitLabEntity CloneEntiry()
    {
        return new GitLabEntity
        {
            Id = this.Id,
            Title = this.Title,
            AvatarUrl = this.AvatarUrl,
            IsPublic = this.IsPublic
        };
    }
}
