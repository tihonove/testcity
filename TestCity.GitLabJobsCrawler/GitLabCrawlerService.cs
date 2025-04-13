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
    public GitLabCrawlerService(GitLabSettings gitLabSettings, ILogger<GitLabCrawlerService> log, JUnitExtractor extractor, WorkerClient workerClient, IHostEnvironment hostEnvironment)
    {
        this.gitLabSettings = gitLabSettings;
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
                                await ProcessProjectJobsAsync(long.Parse(project.Id), clientEx, stopTokenSource.Token);
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
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, Core.GitLab.JobScope.Failed | Core.GitLab.JobScope.Success, perPage: 100, token).Take(200).ToListAsync(token);
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

    private async ValueTask ProcessProjectJobsAsync(long projectId, GitLabExtendedClient clientEx, CancellationToken token)
    {
        log.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, Core.GitLab.JobScope.Failed | Core.GitLab.JobScope.Success, perPage: 100, token).Take(600).ToListAsync(token);
        log.LogInformation("Take last {jobsLength} jobs", jobs.Count);
        var enqueuedCount = 0;
        foreach (var job in jobs)
        {
            if (processedJobSet.ContainsKey((projectId, job.Id)))
            {
                log.LogInformation("Skip job with id: {JobId}", job.Id);
                continue;
            }

            try
            {
                await workerClient.Enqueue(
                    new ProcessJobRunTaskPayload
                    {
                        ProjectId = projectId,
                        JobRunId = job.Id,
                    });
                enqueuedCount++;
                processedJobSet.TryAdd((projectId, job.Id), 0);
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to enqueue job {JobId}", job.Id);
                continue;
            }
        }

        log.LogInformation("Enqueued {JobCount} jobs for {ProjectId}.", enqueuedCount, projectId);
    }

    public void Dispose()
    {
        log.LogInformation("Greaceful shutdown");
        stopTokenSource.Cancel();
    }

    private readonly ConcurrentDictionary<(long, long), byte> processedJobSet = new();
    private readonly GitLabSettings gitLabSettings;
    private readonly CancellationTokenSource stopTokenSource;
    private readonly ILogger<GitLabCrawlerService> log;
    private readonly JUnitExtractor extractor;
    private readonly WorkerClient workerClient;
    private readonly IHostEnvironment hostEnvironment;
}
