using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Kontur.TestCity.GitLabJobsCrawler;
using Kontur.TestCity.Worker.Handlers.Base;
using Microsoft.Extensions.Logging;
using NGitLab;
using NGitLab.Models;

namespace Kontur.TestCity.Worker.Handlers;

public class ProcessJobRunTaskHandler : TaskHandler<ProcessJobRunTaskPayload>
{
    public ProcessJobRunTaskHandler(
        ILogger<ProcessJobRunTaskHandler> logger,
        TestMetricsSender metricsSender,
        WorkerClient workerClient,
        SkbKonturGitLabClientProvider gitLabClientProvider,
        JUnitExtractor extractor)
    {
        this.logger = logger;
        this.metricsSender = metricsSender;
        this.workerClient = workerClient;
        this.extractor = extractor;
        client = gitLabClientProvider.GetClient();
        clientEx = gitLabClientProvider.GetExtendedClient();
    }

    public override bool CanHandle(RawTask task)
    {
        return task.Type == ProcessJobRunTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(ProcessJobRunTaskPayload task, CancellationToken ct)
    {
        logger.LogInformation("Processing job run for project {ProjectId}, job run id: {JobRunId}", task.ProjectId, task.JobRunId);
        try
        {
            var jobProcessor = new GitLabJobProcessor(client, clientEx, extractor, logger);
            var projectInfo = await client.Projects.GetByIdAsync(task.ProjectId, new SingleProjectQuery(), ct);
            var processingResult = await jobProcessor.ProcessJobAsync(task.ProjectId, task.JobRunId);

            if (processingResult.JobInfo != null)
            {
                if (!await TestRunsUploader.IsJobRunIdExists(processingResult.JobInfo.JobRunId))
                {
                    logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", processingResult.JobInfo.JobRunId);
                    await TestRunsUploader.JobInfoUploadAsync(processingResult.JobInfo);
                    await workerClient.Enqueue(new BuildCommitParentsTaskPayload
                    {
                        ProjectId = task.ProjectId,
                        CommitSha = processingResult.JobInfo.CommitSha,
                    });
                    

                    if (processingResult.TestReportData != null)
                    {
                        await TestRunsUploader.UploadAsync(processingResult.JobInfo, processingResult.TestReportData.Runs);
                        var job = await clientEx.GetJobAsync(task.ProjectId,task.JobRunId);
                        await metricsSender.SendAsync(
                            projectInfo,
                            processingResult.JobInfo.BranchName,
                            job,
                            processingResult.TestReportData);
                    }
                }
                else
                {
                    logger.LogInformation("JobRunId '{JobRunId}' already exists. Skipping upload of test runs", processingResult.JobInfo.JobRunId);
                }
            }
            else
            {
                logger.LogInformation("No job info was found for job run id: {JobRunId}", task.JobRunId);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process job {JobId}", task.JobRunId);
        }
    }

    private readonly ILogger<ProcessJobRunTaskHandler> logger;
    private readonly TestMetricsSender metricsSender;
    private readonly WorkerClient workerClient;
    private readonly JUnitExtractor extractor;
    private readonly IGitLabClient client;
    private readonly GitLabExtendedClient clientEx;
}
