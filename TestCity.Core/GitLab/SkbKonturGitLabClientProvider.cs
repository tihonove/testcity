using NGitLab;

namespace Kontur.TestCity.Core.GitLab;

public class SkbKonturGitLabClientProvider(GitLabSettings gitLabSettings)
{
    private readonly GitLabSettings gitLabSettings = gitLabSettings;

    public IGitLabClient GetClient()
    {
        return new GitLabClient("https://git.skbkontur.ru", gitLabSettings.Token);
    }

    public GitLabExtendedClient GetExtendedClient()
    {
        return new GitLabExtendedClient("https://git.skbkontur.ru", gitLabSettings.Token);
    }
}
