using System.Collections.Concurrent;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitLab.Models;
using Kontur.TestCity.Core.GitlabProjects;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class GitLabCrawlerService(GitLabSettings gitLabSettings, WorkerClient workerClient, IHostEnvironment hostEnvironment) : IDisposable
{
    public void Start()
    {
        log.LogInformation("Periodic gitlab jobs update runned");
        Task.Run(async () =>
        {
            if (stopTokenSource.IsCancellationRequested)
            {
                return;
            }

            var gitLabProjectIds = PreconfiguredGitLabProjectsService.GetAllProjects().ToList();
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
                            await ReadLastJobsAndEnqueueToWorker(long.Parse(project.Id), clientEx, stopTokenSource.Token);
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

    private async ValueTask ReadLastJobsAndEnqueueToWorker(long projectId, GitLabExtendedClient clientEx, CancellationToken token)
    {
        log.LogInformation("Pulling jobs for project {ProjectId} to enqueue to worker", projectId);
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, JobScope.Failed | JobScope.Success, perPage: 100, token).Take(200).ToListAsync(token);
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
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, JobScope.Failed | JobScope.Success, perPage: 100, token).Take(600).ToListAsync(token);
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
    private readonly CancellationTokenSource stopTokenSource = new();
    private readonly ILogger log = Log.GetLog<GitLabCrawlerService>();
}
