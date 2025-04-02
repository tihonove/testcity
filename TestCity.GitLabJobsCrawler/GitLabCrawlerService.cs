using System.Collections.Concurrent;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings, TestMetricsSender metricsSender, ILogger<GitLabCrawlerService> log, JUnitExtractor extractor, WorkerClient workerClient)
    {
        this.gitLabSettings = gitLabSettings;
        this.metricsSender = metricsSender;
        this.log = log;
        this.extractor = extractor;
        this.workerClient = workerClient;
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

        foreach (var job in jobs)
        {
            if (processedJobSet.ContainsKey((projectId, job.Id)))
            {
                log.LogInformation("Skip job with id: {JobId}", job.Id);
                continue;
            }

            try
            {
                var processingResult = await jobProcessor.ProcessJobAsync(projectId, job.Id, job);
                try
                {
                    await workerClient.Enqueue(
                        new ProcessJobRunTaskPayload
                        {
                            ProjectId = projectId,
                            JobRunId = job.Id,
                        });
                }
                catch (Exception e)
                {
                    log.LogError(e, "Failed to enqueue job {JobId}", job.Id);
                    throw;
                }
                
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

                    processedJobSet.TryAdd((projectId, job.Id), 0);
                }
                else
                {
                    if (job.Status == NGitLab.JobStatus.Failed || job.Status == NGitLab.JobStatus.Success)
                    {
                        processedJobSet.TryAdd((projectId, job.Id), 0);
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

    private readonly ConcurrentDictionary<(long, long), byte> processedJobSet = new ();
    private readonly GitLabSettings gitLabSettings;
    private readonly TestMetricsSender metricsSender;
    private readonly CancellationTokenSource stopTokenSource;
    private readonly ILogger<GitLabCrawlerService> log;
    private readonly JUnitExtractor extractor;
    private readonly WorkerClient workerClient;
}
