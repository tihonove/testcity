using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings, TestMetricsSender metricsSender, ILogger<GitLabCrawlerService> log, JUnitExtractor extractor)
    {
        this.gitLabSettings = gitLabSettings;
        this.metricsSender = metricsSender;
        this.log = log;
        this.extractor = extractor;
        stopTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        log.LogInformation("Periodic gitlab jobs update runned");
        Task.Run(async () =>
        {
            if (stopTokenSource.IsCancellationRequested)
            {
                return;
            }

            var gitLabProjectIds = GitLabProjectsService.GetAllProjects().Select(x => x.Id).Select(x => int.Parse(x)).ToList();
            var gitLabClientProvider = new SkbKonturGitLabClientProvider(gitLabSettings);
            var client = gitLabClientProvider.GetClient();
            await Task.WhenAll(gitLabProjectIds.Select(projectId =>
            {
                return Task.Run(async () =>
                {
                    while (!stopTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            await ProcessProjectJobsAsync(projectId, client, stopTokenSource.Token);
                        }
                        catch (Exception e)
                        {
                            log.LogError(e, "Failed to update gitlab artifacts");
                        }

                        await Task.Delay(TimeSpan.FromMinutes(1), stopTokenSource.Token);
                    }
                });
            }).ToArray());
        });
    }

    private async ValueTask ProcessProjectJobsAsync(int projectId, NGitLab.IGitLabClient client, CancellationToken token)
    {
        log.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var jobProcessor = new GitLabJobProcessor(client, extractor, log);
        var jobsClient = client.GetJobs(projectId);
        var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery(), token);
        var jobsQuery = new JobQuery
        {
            PerPage = 100,
            Scope = JobScopeMask.All &
                ~JobScopeMask.Canceled &
                ~JobScopeMask.Skipped &
                ~JobScopeMask.Pending &
                ~JobScopeMask.Running &
                ~JobScopeMask.Created,
        };
        var jobs = jobsClient.GetJobsAsync(jobsQuery).Take(600).ToArray();
        log.LogInformation("Take last {jobsLength} jobs", jobs.Length);
        var processedJobIds = new List<long>();

        foreach (var job in jobs)
        {
            if (processedJobSet.Contains(job.Id))
            {
                log.LogInformation("Skip job with id: {JobId}", job.Id);
                continue;
            }

            try
            {
                var processingResult = await jobProcessor.ProcessJobAsync(projectId, job.Id, job);
                if (processingResult.JobInfo != null)
                {
                    if (!await TestRunsUploader.IsJobRunIdExists(processingResult.JobInfo.JobRunId))
                    {
                        log.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", processingResult.JobInfo.JobRunId);
                        await TestRunsUploader.JobInfoUploadAsync(processingResult.JobInfo);

                        if (processingResult.TestReportData != null)
                        {
                            await TestRunsUploader.UploadAsync(processingResult.JobInfo, processingResult.TestReportData.Runs);
                            await metricsSender.SendAsync(projectInfo, processingResult.JobInfo.BranchName, job, processingResult.TestReportData);
                        }
                    }
                    else
                    {
                        log.LogInformation("JobRunId '{JobRunId}' exists. Skip uploading test runs", processingResult.JobInfo.JobRunId);
                    }

                    processedJobIds.Add(job.Id);
                }
                else
                {
                    if (job.Status == NGitLab.JobStatus.Failed || job.Status == NGitLab.JobStatus.Success)
                    {
                        processedJobIds.Add(job.Id);
                    }
                }
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to process job {JobId}", job.Id);
                continue;
            }
        }

        log.LogInformation("Processed {JobCount} for {ProjectId}. First job: {FirstJobId}, Last job: {LastJobId}", jobs.Length, projectId, jobs.FirstOrDefault()?.Id, jobs.LastOrDefault()?.Id);
    }

    public void Dispose()
    {
        log.LogInformation("Greaceful shutdown");
        stopTokenSource.Cancel();
    }

    private readonly HashSet<long> processedJobSet = new HashSet<long>();
    private readonly GitLabSettings gitLabSettings;
    private readonly TestMetricsSender metricsSender;
    private readonly CancellationTokenSource stopTokenSource;
    private readonly ILogger<GitLabCrawlerService> log;
    private readonly JUnitExtractor extractor;
}
