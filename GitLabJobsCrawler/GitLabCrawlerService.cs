using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Api;

public class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService()
    {
        CrawlingProc();
    }

    public void CrawlingProc()
    {
        log.Info("Test log message");
    }

    public void Dispose()
    {
        log.Info("Greaceful shutdown");
    }

    private readonly ILog log = LogProvider.Get().ForContext<GitLabCrawlerService>();
}
