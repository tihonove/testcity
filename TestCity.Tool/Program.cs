using System.Text;
using ClickHouse.Client.Utility;
using dotenv.net;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitlabProjects;
using Kontur.TestCity.Core.JobProcessing;
using Kontur.TestCity.Core.JUnit;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;
using NGitLab.Models;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();
Log.ConfigureGlobalLogProvider(loggerFactory);

var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
var clientEx = clientProvider.GetExtendedClient();
var client = clientProvider.GetClient();
var messageQueueClient = KafkaMessageQueueClient.CreateDefault(loggerFactory.CreateLogger<KafkaMessageQueueClient>());
var workreClient = new WorkerClient(messageQueueClient);
var connectionFactory = new ConnectionFactory();
var connection = connectionFactory.CreateConnection();
var testCityDatabase = new TestCityDatabase(connectionFactory);
var gitLabProjectsService = new GitLabProjectsService(testCityDatabase);
var projects = await gitLabProjectsService.GetAllProjects();
var projectJobTypesCache = new ProjectJobTypesCache(testCityDatabase);
var extractor = new JUnitExtractor();
var workerClient = new WorkerClient(messageQueueClient);
var ct = CancellationToken.None;

logger.LogInformation("Starting to process projects");
foreach (var project in projects)
{
    await ProcessTasksInInProgressJobs(project);
}

logger.LogInformation("Processing completed for all projects");

async Task ProcessTasksInInProgressJobs(Kontur.TestCity.Core.GitlabProjects.GitLabProject project)
{
    logger.LogInformation("Processing project {ProjectId}: {ProjectTitle}", project.Id, project.Title);
    var jobs = await testCityDatabase.InProgressJobInfo.GetAllByProjectIdAsync(project.Id);
    foreach (var unprocessedJob in jobs) 
    {
        if (await testCityDatabase.JobInfo.ExistsAsync(unprocessedJob.JobRunId))
        {
            continue;
        }
        Console.WriteLine($"{unprocessedJob.JobRunId}");

        var projectId = long.Parse(unprocessedJob.ProjectId);
        var jobRunId = long.Parse(unprocessedJob.JobRunId);
        // logger.LogInformation("Processing job run for project {ProjectId}, job run id: {JobRunId}", projectId, jobRunId);
        try
        {
            if (await testCityDatabase.JobInfo.ExistsAsync(jobRunId.ToString()))
            {
                logger.LogInformation("JobRunId '{JobRunId}' in '{ProjectId}' already exists. Skipping upload of test runs", jobRunId, projectId);
                continue;
            }
            var needProcessFailedJob = await projectJobTypesCache.JobTypeExistsAsync(projectId.ToString(), unprocessedJob.JobId.ToString(), ct);
            logger.LogInformation("JobId '{JobId}'. NeedProcessFailedJob: {needProcessFailedJob}", unprocessedJob.JobId, needProcessFailedJob);
            var jobProcessor = new GitLabJobProcessor(client, clientEx, extractor, logger);
            var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery(), ct);
            var processingResult = await jobProcessor.ProcessJobAsync(projectId, jobRunId, null, needProcessFailedJob);

            if (processingResult.JobInfo != null)
            {
                logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", processingResult.JobInfo.JobRunId);
                logger.LogInformation("CustomStatusMessage: {CustomStatusMessage}", processingResult.JobInfo.CustomStatusMessage);
                logger.LogInformation("State: {State}", processingResult.JobInfo.State.ToString());
                await testCityDatabase.JobInfo.InsertAsync(processingResult.JobInfo);
                if (processingResult.JobInfo.CommitSha != null)
                {
                    await workerClient.Enqueue(new BuildCommitParentsTaskPayload
                    {
                        ProjectId = projectId,
                        CommitSha = processingResult.JobInfo.CommitSha,
                    });
                }
                else
                {
                    logger.LogInformation("JobRunId '{JobRunId}' does not have commit sha. Skipping upload of commit parents", processingResult.JobInfo.JobRunId);
                }

                if (processingResult.TestReportData != null)
                {
                    // await testCityDatabase.TestRuns.InsertBatchAsync(processingResult.JobInfo, processingResult.TestReportData.Runs);
                }
            }
            else
            {
                logger.LogInformation("No job info was found for job run id: {JobRunId}", jobRunId);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process job {JobId}", jobRunId);
        }


    }
}

#pragma warning disable CS8321 // Local function is declared but never used
async Task ProcessCommits(Kontur.TestCity.Core.GitlabProjects.GitLabProject project)
{
    logger.LogInformation("Processing project {ProjectId}: {ProjectTitle}", project.Id, project.Title);
    int commitCount = 0;

    try
    {
        var connectionFactory = new ConnectionFactory();
        var connection = connectionFactory.CreateConnection();
        var query = $"SELECT DISTINCT CommitSha FROM JobInfo WHERE ProjectId = '{project.Id}'";
        using var reader = await connection.ExecuteReaderAsync(query);

        var commitHashes = new List<string>();
        while (await reader.ReadAsync())
        {
            var commitSha = reader.GetString(0);
            commitHashes.Add(commitSha);
        }

        logger.LogInformation("Found {CommitCount} unique commits in ClickHouse for project {ProjectId}",
            commitHashes.Count, project.Id);

        foreach (var commitSha in commitHashes)
        {
            logger.LogDebug("Processing commit {CommitSha} from ClickHouse", commitSha);

            try
            {
                await workreClient.Enqueue(new BuildCommitParentsTaskPayload
                {
                    ProjectId = long.Parse(project.Id),
                    CommitSha = commitSha
                });
                logger.LogDebug("Successfully enqueued task for commit {CommitSha}", commitSha);
                commitCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue task for commit {CommitSha} in project {ProjectId}",
                    commitSha, project.Id);
            }
        }

        logger.LogInformation("Processed {CommitCount} commits for project {ProjectId}", commitCount, project.Id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing project {ProjectId}: {ProjectTitle}", project.Id, project.Title);
    }
}
#pragma warning restore CS8321 // Local function is declared but never used
