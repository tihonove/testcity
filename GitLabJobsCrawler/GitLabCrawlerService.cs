using Kontur.TestAnalytics.Api;
using Kontur.TestAnalytics.Core;
using Kontur.TestAnalytics.GitLabJobsCrawler.Services;
using Kontur.TestAnalytics.Reporter.Client;
using NGitLab.Models;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.GitLabJobsCrawler;

public sealed class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings, TestMetricsSender metricsSender)
    {
        this.gitLabSettings = gitLabSettings;
        this.metricsSender = metricsSender;
        stopTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        log.Info("Perioding gitlab jobs update runned");
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
                    log.Info("Start pulling gitlab job artifacts");
                    await PullGitLabJobArtifactsAndPushIntoTestAnalytics(stopTokenSource.Token);
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to update gitlab artifacts");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stopTokenSource.Token);
            }
        });
    }

    private async Task PullGitLabJobArtifactsAndPushIntoTestAnalytics(CancellationToken token)
    {
        var gitLabClientProvider = new SkbKonturGitLabClientProvider(gitLabSettings);
        var gitLabProjectIds = GitLabProjectsService.GetAllProjects().Select(x => x.Id).Except(Second).Select(x => int.Parse(x)).ToList();

        foreach (var projectId in gitLabProjectIds)
        {
            log.Info($"Pulling jobs for project {projectId}");
            var client = gitLabClientProvider.GetClient();
            var jobsClient = client.GetJobs(projectId);
            var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery(), token);
            var jobsQuery = new NGitLab.Models.JobQuery
            {
                PerPage = 300,
                Scope = JobScopeMask.All &
                    ~JobScopeMask.Canceled &
                    ~JobScopeMask.Skipped &
                    ~JobScopeMask.Pending &
                    ~JobScopeMask.Running &
                    ~JobScopeMask.Created,
            };
            var jobs = jobsClient.GetJobsAsync(jobsQuery).Take(300).ToArray();
            log.Info($"Take last {jobs.Length} jobs");

            foreach (var job in jobs)
            {
                if (processedJobSet.Contains(job.Id))
                {
                    log.Info($"Skip job with id: {job.Id}");
                    continue;
                }

                log.Info($"Start processing job with id: {job.Id}");
                if (job.Artifacts != null)
                {
                    try
                    {
                        var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                        log.Info($"Artifact size for job with id: {job.Id}. Size: {artifactContents.Length} bytes");
                        var extractor = new JUnitExtractor();
                        var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                        if (extractResult != null)
                        {
                            var refId = await client.BranchOrRef(projectId, job.Ref);
                            var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult.Counters, projectId.ToString());
                            if (!await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId))
                            {
                                log.Info($"JobRunId '{jobInfo.JobRunId}' does not exist. Uploading test runs");
                                await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                                await TestRunsUploader.UploadAsync(jobInfo, extractResult.Runs);

                                metricsSender.Send(projectInfo, refId, job, extractResult);
                            }
                            else
                            {
                                log.Info($"JobRunId '{jobInfo.JobRunId}' exists. Skip uploading test runs");
                            }

                            processedJobSet.Add(job.Id);
                        }
                    }
                    catch (Exception exception)
                    {
                        log.Warn(exception, $"Failed to read artifact for {job.Id}");
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

    private readonly HashSet<long> processedJobSet = new HashSet<long>();
    private readonly ILog log = LogProvider.Get().ForContext<GitLabCrawlerService>();
    private readonly GitLabSettings gitLabSettings;
    private readonly TestMetricsSender metricsSender;
    private readonly CancellationTokenSource stopTokenSource;
    private static readonly string[] Second = new[] { "17358", "19371" };
}
