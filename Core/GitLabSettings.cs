namespace Kontur.TestAnalytics.Core;

public class GitLabSettings
{
    public string GitLabToken { get; set; }

    public static GitLabSettings Default => DefaultInstance.Value;

    private static readonly Lazy<GitLabSettings> DefaultInstance =
        new (static () => new GitLabSettings() { GitLabToken = Environment.GetEnvironmentVariable("GITLAB_TOKEN") ?? "NoToken" });
}
