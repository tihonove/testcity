using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Api;

public class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings)
    {
        this.gitLabSettings = gitLabSettings;
        this.stopTokenSource = new CancellationTokenSource();
    }

    public void Start() 
    {
        log.Info($"Perioding gitlab jobs update runned");
        Task.Run(async () => {
            if (stopTokenSource.IsCancellationRequested) {
                return;
            }
            while (!stopTokenSource.IsCancellationRequested) {
                try {
                    log.Info($"Start pulling gitlab job artifacts");
                    await PullGitLabJobArtifactsAndPushIntoTestAnalytics(stopTokenSource.Token);
                }
                catch (Exception e) {
                    log.Error(e, $"Failed to update gitlab artifacts");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stopTokenSource.Token);
            }
        });
    }

    private async Task PullGitLabJobArtifactsAndPushIntoTestAnalytics(CancellationToken token)
    {
        await Task.Delay(10000, token);
    }

    public void Dispose()
    {
        log.Info("Greaceful shutdown");
        stopTokenSource.Cancel();
    }

    private readonly ILog log = LogProvider.Get().ForContext<GitLabCrawlerService>();
    private readonly GitLabSettings gitLabSettings;
    private readonly CancellationTokenSource stopTokenSource;
}
