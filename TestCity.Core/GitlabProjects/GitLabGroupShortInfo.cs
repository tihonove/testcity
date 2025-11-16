namespace TestCity.Core.GitlabProjects;

public class GitLabGroupShortInfo : GitLabEntity
{
    public GitLabGroupShortInfo CloneShortInfo()
    {
        return new GitLabGroupShortInfo
        {
            Id = this.Id,
            Title = this.Title,
            AvatarUrl = this.AvatarUrl,
            IsPublic = this.IsPublic
        };
    }

}
