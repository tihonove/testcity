using NGitLab;

namespace TestCity.Core.GitLab;

public class SkbKonturGitLabClientProvider(GitLabSettings gitLabSettings)
{
    public IGitLabClient GetClient()
    {
        return new GitLabClient(gitLabSettings.Url.ToString().TrimEnd('/'), gitLabSettings.Token);
    }

    public GitLabExtendedClient GetExtendedClient()
    {
        return new GitLabExtendedClient(gitLabSettings.Url, gitLabSettings.Token);
    }
}
