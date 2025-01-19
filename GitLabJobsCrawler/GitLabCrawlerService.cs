using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Api;

public class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings)
    {
        this.gitLabSettings = gitLabSettings;
        CrawlingProc();
    }

    public void CrawlingProc()
    {
        log.Info($"Test log message {string.IsNullOrWhiteSpace(gitLabSettings.GitLabToken).ToString()}");
        log.Error("Test error message");
    }

    public void Dispose()
    {
        log.Info("Greaceful shutdown");
    }

    private readonly ILog log = LogProvider.Get().ForContext<GitLabCrawlerService>();
    private readonly GitLabSettings gitLabSettings;
}
