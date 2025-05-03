using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.JobProcessing;
using Kontur.TestCity.Core.JUnit;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;
using NGitLab;
using NGitLab.Models;
using OpenTelemetry;

namespace Kontur.TestCity.Worker.Handlers;

public class ProcessJobRunTaskHandler(
    TestMetricsSender metricsSender,
    SkbKonturGitLabClientProvider gitLabClientProvider,
    TestCityDatabase testCityDatabase,
    JUnitExtractor extractor,
    ProjectJobTypesCache projectJobTypesCache,
    CommitParentsBuilderService commitParentsBuilder
    ) : TaskHandler<ProcessJobRunTaskPayload>
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == ProcessJobRunTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(ProcessJobRunTaskPayload task, CancellationToken ct)
    {
        Baggage.SetBaggage("ProjectId", task.ProjectId.ToString());
        Baggage.SetBaggage("JobRunId", task.JobRunId.ToString());
        logger.LogInformation("Processing job run for project {ProjectId}, job run id: {JobRunId}", task.ProjectId, task.JobRunId);
        try
        {
            if (await testCityDatabase.JobInfo.ExistsAsync(task.JobRunId.ToString()))
            {
                logger.LogInformation("JobRunId '{JobRunId}' in '{ProjectId}' already exists. Skipping upload of test runs", task.JobRunId, task.ProjectId);
                return;
            }
            var job = await clientEx.GetJobAsync(task.ProjectId, task.JobRunId);
            Baggage.SetBaggage("JobId", job.Name.ToString());
            if (job.Ref is not null)
                Baggage.SetBaggage("Ref", job.Ref);
            if (job.Commit?.Id is not null)
                await commitParentsBuilder.BuildCommitParent(task.ProjectId, job.Commit.Id, ct);
            var needProcessFailedJob = await projectJobTypesCache.JobTypeExistsAsync(task.ProjectId.ToString(), job.Name, ct);
            var jobProcessor = new GitLabJobProcessor(client, clientEx, extractor, logger);
            var projectInfo = await client.Projects.GetByIdAsync(task.ProjectId, new SingleProjectQuery(), ct);
            var processingResult = await jobProcessor.ProcessJobAsync(task.ProjectId, task.JobRunId, job, needProcessFailedJob);

            if (processingResult.JobInfo != null)
            {
                logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", processingResult.JobInfo.JobRunId);
                if (job.Commit?.Id is not null && job.Ref is not null)
                    processingResult.JobInfo.ChangesSinceLastRun = await testCityDatabase.GetCommitChangesAsync(job.Commit.Id, job.Name, job.Ref, ct);
                await testCityDatabase.JobInfo.InsertAsync(processingResult.JobInfo);
                if (processingResult.TestReportData != null)
                {
                    await testCityDatabase.TestRuns.InsertBatchAsync(processingResult.JobInfo, processingResult.TestReportData.Runs);
                    await metricsSender.SendAsync(
                        projectInfo,
                        processingResult.JobInfo.BranchName,
                        job,
                        processingResult.TestReportData);
                }
            }
            else
            {
                logger.LogInformation("No job info was found for job run id: {JobRunId}", task.JobRunId);
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            if (httpRequestException.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogInformation("Job run id: {JobRunId} is not accessible. Skipping upload of test runs", task.JobRunId);
            }
            else
            {
                throw;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process job {JobId}", task.JobRunId);
            throw;
        }
    }

    private readonly ILogger logger = Log.GetLog<ProcessJobRunTaskHandler>();
    private readonly IGitLabClient client = gitLabClientProvider.GetClient();
    private readonly GitLabExtendedClient clientEx = gitLabClientProvider.GetExtendedClient();
}
