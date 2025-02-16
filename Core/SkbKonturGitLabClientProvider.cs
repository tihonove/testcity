using NGitLab;

namespace Kontur.TestAnalytics.Core;

public class SkbKonturGitLabClientProvider(GitLabSettings gitLabSettings)
{
    private readonly GitLabSettings gitLabSettings = gitLabSettings;

    public IGitLabClient GetClient()
    {
        return new GitLabClient("https://git.skbkontur.ru", gitLabSettings.GitLabToken);
    }
}
