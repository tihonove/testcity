namespace TestCity.Core.GitLab;

public class GitLabSettings
{
    public required Uri Url { get; set; }
    public required string Token { get; set; }

    public static GitLabSettings Default => new()
    {
        Url = new Uri(Environment.GetEnvironmentVariable("GITLAB_URL") ?? throw new InvalidOperationException("GITLAB_URL environment variable is not set"), UriKind.Absolute),
        Token = Environment.GetEnvironmentVariable("GITLAB_TOKEN") ?? "NoExistingToken",
    };
}
