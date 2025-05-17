namespace TestCity.Core.GitLab;

public class GitLabSettings
{
    public required string Token { get; set; }

    public static GitLabSettings Default => new ()
    {
        Token = Environment.GetEnvironmentVariable("GITLAB_TOKEN") ?? ""
    };
}
