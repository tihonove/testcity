using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
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
        GitLabSettings gitLabSettings,
        TestMetricsSender metricsSender,
        JUnitExtractor extractor)
    {
        this.logger = logger;
        this.metricsSender = metricsSender;
        this.extractor = extractor;
        var gitLabClientProvider = new SkbKonturGitLabClientProvider(gitLabSettings);
        client = gitLabClientProvider.GetClient();
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
            var jobProcessor = new GitLabJobProcessor(client, extractor, logger);
            var projectInfo = await client.Projects.GetByIdAsync(task.ProjectId, new SingleProjectQuery(), ct);
            var processingResult = await jobProcessor.ProcessJobAsync(task.ProjectId, task.JobRunId);

            if (processingResult.JobInfo != null)
            {
                if (!await TestRunsUploader.IsJobRunIdExists(processingResult.JobInfo.JobRunId))
                {
                    logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", processingResult.JobInfo.JobRunId);
                    // await TestRunsUploader.JobInfoUploadAsync(processingResult.JobInfo);

                    if (processingResult.TestReportData != null)
                    {
                        // await TestRunsUploader.UploadAsync(processingResult.JobInfo, processingResult.TestReportData.Runs);
                        // var job = await client.GetJobs(task.ProjectId).GetAsync(task.JobRunId);
                        // await metricsSender.SendAsync(
                        //     projectInfo,
                        //     processingResult.JobInfo.BranchName,
                        //     job,
                        //     processingResult.TestReportData);
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
    private readonly JUnitExtractor extractor;
    private readonly IGitLabClient client;
}
