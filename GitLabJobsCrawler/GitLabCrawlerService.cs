using Kontur.TestAnalytics.Api;
using Kontur.TestAnalytics.Reporter.Client;
using NGitLab;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.GitLabJobsCrawler;

public class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings)
    {
        this.gitLabSettings = gitLabSettings;
        stopTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        log.Info($"Perioding gitlab jobs update runned");
        Task.Run(async () =>
        {
            if (stopTokenSource.IsCancellationRequested)
            {
                return;
            }
            while (!stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    log.Info($"Start pulling gitlab job artifacts");
                    await PullGitLabJobArtifactsAndPushIntoTestAnalytics(stopTokenSource.Token);
                }
                catch (Exception e)
                {
                    log.Error(e, $"Failed to update gitlab artifacts");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stopTokenSource.Token);
            }
        });
    }

    private async Task PullGitLabJobArtifactsAndPushIntoTestAnalytics(CancellationToken token)
    {
        var processedJobSet = new HashSet<long>();
        var gitLabProjectIds = new[] { 182 };

        foreach (var projectId in gitLabProjectIds)
        {
            log.Info($"Pulling jobs for project {projectId}");
            var client = new GitLabClient("https://git.skbkontur.ru", gitLabSettings.GitLabToken);
            var jobsClient = client.GetJobs(projectId);
            var jobsQuery = new NGitLab.Models.JobQuery
            {
                PerPage = 300,
                Scope = NGitLab.Models.JobScopeMask.All &
                    ~NGitLab.Models.JobScopeMask.Canceled &
                    ~NGitLab.Models.JobScopeMask.Skipped &
                    ~NGitLab.Models.JobScopeMask.Pending &
                    ~NGitLab.Models.JobScopeMask.Running &
                    ~NGitLab.Models.JobScopeMask.Created

            };
            var jobs = jobsClient.GetJobsAsync(jobsQuery).Take(300).ToArray();
            log.Info($"Take last {jobs.Length} jobs");

            foreach (var job in jobs)
            {
                if (processedJobSet.Contains(job.Id)) {
                    log.Info($"Skip job with id: {job.Id}");    
                    continue;
                }
                log.Info($"Start processing job with id: {job.Id}");
                if (job.Artifacts != null)
                {
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    log.Info($"Artifact size for job with id: {job.Id}. Size: {artifactContents.Length} bytes");
                    var extractor = new JUnitExtractor();
                    var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (extractResult != null)
                    {
                        var jobInfo = GitLabHelpers.GetFullJobInfo(job, extractResult.Counters);
                        if (!await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId))
                        {
                            await TestRunsUploader.UploadAsync(jobInfo, extractResult.Runs);
                            await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                        }
                        processedJobSet.Add(job.Id);
                    }
                }
            }
        }
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
