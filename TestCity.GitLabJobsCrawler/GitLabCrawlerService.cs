using System.Collections.Concurrent;
using TestCity.Core.GitLab;
using TestCity.Core.GitLab.Models;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Logging;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;

namespace TestCity.GitLabJobsCrawler;

public sealed class GitLabCrawlerService(GitLabSettings gitLabSettings, WorkerClient workerClient, GitLabProjectsService gitLabProjectsService, IHostEnvironment hostEnvironment) : IDisposable
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

            var gitLabProjectIds = (await gitLabProjectsService.GetAllProjects()).ToList();
            if (hostEnvironment.IsDevelopment())
                gitLabProjectIds = gitLabProjectIds.Where(x => x.Id == "2680" || x.Id == "24783" || x.Id == "70134580").ToList();
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

    private async ValueTask ProcessProjectJobsAsync(long projectId, GitLabExtendedClient clientEx, CancellationToken token)
    {
        log.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var jobs = await clientEx
            .GetAllProjectJobsAsync(projectId, JobScope.Failed | JobScope.Success | JobScope.Running | JobScope.Canceled, perPage: 100, token)
            .Take(600)
            .ToListAsync(token);
        jobs.Reverse();
        log.LogInformation("Take last {jobsLength} jobs", jobs.Count);
        var enqueuedCount = 0;
        foreach (var job in jobs)
        {
            if (job.Status == JobStatus.Running)
            {
                if (processedRunningJobSet.ContainsKey((projectId, job.Id)))
                {
                    log.LogDebug("Skip job with id: {JobId}", job.Id);
                    continue;
                }

                try
                {
                    await workerClient.Enqueue(
                        new ProcessInProgressJobTaskPayload
                        {
                            ProjectId = projectId,
                            JobRunId = job.Id,
                        });
                    enqueuedCount++;
                    processedRunningJobSet.TryAdd((projectId, job.Id), 0);
                    log.LogInformation("Enqueued running job {JobId} for project {ProjectId}", job.Id, projectId);
                }
                catch (Exception exception)
                {
                    log.LogError(exception, "Failed to enqueue job {JobId}", job.Id);
                    continue;
                }
            }
            else
            {
                if (processedCompletedJobSet.ContainsKey((projectId, job.Id)))
                {
                    log.LogDebug("Skip job with id: {JobId}", job.Id);
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
                    processedCompletedJobSet.TryAdd((projectId, job.Id), 0);
                    log.LogInformation("Enqueued completed job {JobId} for project {ProjectId}", job.Id, projectId);
                }
                catch (Exception exception)
                {
                    log.LogError(exception, "Failed to enqueue job {JobId}", job.Id);
                    continue;
                }
            }
        }

        log.LogInformation("Enqueued {JobCount} jobs for {ProjectId}.", enqueuedCount, projectId);
    }

    public void Dispose()
    {
        log.LogInformation("Greaceful shutdown");
        stopTokenSource.Cancel();
    }

    private readonly ConcurrentDictionary<(long, long), byte> processedCompletedJobSet = new();
    private readonly ConcurrentDictionary<(long, long), byte> processedRunningJobSet = new();
    private readonly CancellationTokenSource stopTokenSource = new();
    private readonly ILogger log = Log.GetLog<GitLabCrawlerService>();
}
