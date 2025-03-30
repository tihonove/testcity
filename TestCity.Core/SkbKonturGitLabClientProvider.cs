using NGitLab;

namespace Kontur.TestCity.Core;

public class SkbKonturGitLabClientProvider(GitLabSettings gitLabSettings)
{
    private readonly GitLabSettings gitLabSettings = gitLabSettings;

    public IGitLabClient GetClient()
    {
        return new GitLabClient("https://git.skbkontur.ru", gitLabSettings.GitLabToken);
    }
}
