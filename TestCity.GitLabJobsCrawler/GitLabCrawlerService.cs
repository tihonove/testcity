using System.Collections.Concurrent;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class GitLabCrawlerService : IDisposable
{
    public GitLabCrawlerService(GitLabSettings gitLabSettings, TestMetricsSender metricsSender, ILogger<GitLabCrawlerService> log, JUnitExtractor extractor, WorkerClient workerClient, IHostEnvironment hostEnvironment)
    {
        this.gitLabSettings = gitLabSettings;
        this.metricsSender = metricsSender;
        this.log = log;
        this.extractor = extractor;
        this.workerClient = workerClient;
        this.hostEnvironment = hostEnvironment;
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

            var gitLabProjectIds = GitLabProjectsService.GetAllProjects().ToList();
            var gitLabClientProvider = new SkbKonturGitLabClientProvider(gitLabSettings);
            var client = gitLabClientProvider.GetClient();
            var clientEx = gitLabClientProvider.GetExtendedClient();
            await Task.WhenAll(gitLabProjectIds.Select(project =>
            {
                return Task.Run(async () =>
                {
                    if (project.UseHooks)
                    {
                        if (hostEnvironment.IsDevelopment())
                        {
                            await ReadLastJobsAndEnueueToWorker(long.Parse(project.Id), clientEx, stopTokenSource.Token);
                        }
                    }
                    else
                    {
                        while (!stopTokenSource.IsCancellationRequested)
                        {
                            try
                            {
                                await ProcessProjectJobsAsync(long.Parse(project.Id), client, clientEx, stopTokenSource.Token);
                            }
                            catch (Exception e)
                            {
                                log.LogError(e, "Failed to update gitlab artifacts");
                            }

                            await Task.Delay(TimeSpan.FromMinutes(1), stopTokenSource.Token);
                        }
                    }
                });
            }).ToArray());
        });
    }

    private async ValueTask ReadLastJobsAndEnueueToWorker(long projectId, GitLabExtendedClient clientEx, CancellationToken token)
    {
        log.LogInformation("Pulling jobs for project {ProjectId} to enqueue to worker", projectId);
        const Core.GitLab.JobScope scopes = Core.GitLab.JobScope.All &
                ~Core.GitLab.JobScope.Canceled &
                ~Core.GitLab.JobScope.Skipped &
                ~Core.GitLab.JobScope.Pending &
                ~Core.GitLab.JobScope.Running &
                ~Core.GitLab.JobScope.Created;
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, scopes, perPage: 100, token).Take(200).ToListAsync(token);
        log.LogInformation("Take last {jobsLength} jobs", jobs.Count);

        foreach (var job in jobs)
        {
            try
            {
                await workerClient.Enqueue(
                    new ProcessJobRunTaskPayload
                    {
                        ProjectId = projectId,
                        JobRunId = job.Id,
                    });
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to enqueue job {JobId}", job.Id);
            }
        }
    }

    private async ValueTask ProcessProjectJobsAsync(long projectId, NGitLab.IGitLabClient client, GitLabExtendedClient clientEx, CancellationToken token)
    {
        log.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var jobProcessor = new GitLabJobProcessor(client, clientEx, extractor, log);
        var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery(), token);
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, Core.GitLab.JobScope.Failed | Core.GitLab.JobScope.Success, perPage: 100, token).Take(600).ToListAsync(token);
        log.LogInformation("Take last {jobsLength} jobs", jobs.Count);

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
                if (processingResult.JobInfo != null)
                {
                    if (!await TestRunsUploader.IsJobRunIdExists(processingResult.JobInfo.JobRunId))
                    {
                        log.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", processingResult.JobInfo.JobRunId);
                        await TestRunsUploader.JobInfoUploadAsync(processingResult.JobInfo);
                        await workerClient.Enqueue(new BuildCommitParentsTaskPayload
                        {
                            ProjectId = projectId,
                            CommitSha = processingResult.JobInfo.CommitSha,
                        });

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
                    if (job.Status == Core.GitLab.Models.JobStatus.Failed || job.Status == Core.GitLab.Models.JobStatus.Success)
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

        log.LogInformation("Processed {JobCount} for {ProjectId}. First job: {FirstJobId}, Last job: {LastJobId}", jobs.Count, projectId, jobs.FirstOrDefault()?.Id, jobs.LastOrDefault()?.Id);
    }

    public void Dispose()
    {
        log.LogInformation("Greaceful shutdown");
        stopTokenSource.Cancel();
    }

    private readonly ConcurrentDictionary<(long, long), byte> processedJobSet = new();
    private readonly GitLabSettings gitLabSettings;
    private readonly TestMetricsSender metricsSender;
    private readonly CancellationTokenSource stopTokenSource;
    private readonly ILogger<GitLabCrawlerService> log;
    private readonly JUnitExtractor extractor;
    private readonly WorkerClient workerClient;
    private readonly IHostEnvironment hostEnvironment;
}
