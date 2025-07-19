using System.Text;
using ClickHouse.Client.Utility;
using dotenv.net;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.JobProcessing;
using TestCity.Core.JUnit;
using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;
using TestCity.Core.Extensions;
using Microsoft.Extensions.Logging;
using NGitLab.Models;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "hh:mm:ss ";
}));
var logger = loggerFactory.CreateLogger<Program>();
Log.ConfigureGlobalLogProvider(loggerFactory);

var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
var clientEx = clientProvider.GetExtendedClient();
var client = clientProvider.GetClient();
var messageQueueClient = KafkaMessageQueueClient.CreateDefault(loggerFactory.CreateLogger<KafkaMessageQueueClient>());
var workreClient = new WorkerClient(messageQueueClient);
var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
var connection = connectionFactory.CreateConnection();
var testCityDatabase = new TestCityDatabase(connectionFactory);
var gitLabProjectsService = new GitLabProjectsService(testCityDatabase, clientProvider);
var projects = await gitLabProjectsService.GetAllProjects();
var projectJobTypesCache = new ProjectJobTypesCache(testCityDatabase);
var extractor = new JUnitExtractor();
var workerClient = new WorkerClient(messageQueueClient);
var ct = CancellationToken.None;

logger.LogInformation("Starting to process projects");
await CopyData(new ConnectionFactory(ClickHouseConnectionSettings.Default), new ConnectionFactory(new ClickHouseConnectionSettings()
{
    Host = "vm-ch-tstct-1",
    Port = 8123,
    Database = "default",
    Username = "svc_testcity_gitlab",
    Password = "***"
}));
logger.LogInformation("Processing completed for all projects");

#pragma warning disable CS8321 // Local function is declared but never used
async Task ProcessTasksInInProgressJobs(GitLabProject project)
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

async Task ProcessCommits(GitLabProject project)
{
    logger.LogInformation("Processing project {ProjectId}: {ProjectTitle}", project.Id, project.Title);
    int commitCount = 0;

    try
    {
        var connSettings = new ClickHouseConnectionSettings
        {
            Host = "localhost",
            Port = 9000,
            Database = "default",
            Username = "default",
            Password = ""
        };

        var connFactory = new ConnectionFactory(connSettings);
        var connection = connFactory.CreateConnection();
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

async Task CopyData(ConnectionFactory sourceConnectionFactory, ConnectionFactory targetConnectionFactory)
{
    var sourceDb = new TestCityDatabase(sourceConnectionFactory);
    var targetDb = new TestCityDatabase(targetConnectionFactory);
    var logger = loggerFactory.CreateLogger<Program>();

    // Перенос GitLabEntities (маленькая таблица, используем весь набор данных сразу)
    logger.LogInformation("Starting GitLabEntities migration");
    var gitLabEntities = await sourceDb.GitLabEntities.GetAllEntitiesAsync().ToListAsync();
    logger.LogInformation("Received {Count} GitLabEntities records", gitLabEntities.Count);
    await targetDb.GitLabEntities.UpsertEntitiesAsync(gitLabEntities);
    logger.LogInformation("GitLabEntities migration completed");

    // Перенос JobInfo (потоково, пакетами по 1000)
    logger.LogInformation("Starting JobInfo migration");
    int jobInfoCount = 0;
    await foreach (var batch in sourceDb.JobInfo.GetAllAsync().Batches(1000))
    {
        jobInfoCount += batch.Count;
        logger.LogInformation("Received batch of {Count} JobInfo records", batch.Count);
        await targetDb.JobInfo.InsertAsync(batch);
        logger.LogInformation("Migrated {Count} JobInfo records", jobInfoCount);
    }
    logger.LogInformation("JobInfo migration completed. Total migrated: {Count} records", jobInfoCount);

    logger.LogInformation("Starting CommitParents migration");
    int commitParentsCount = 0;
    await foreach (var batch in sourceDb.CommitParents.GetAllAsync().Batches(10000))
    {
        await Retry.Action(() => targetDb.CommitParents.InsertBatchAsync(batch), TimeSpan.FromMinutes(10));
        commitParentsCount += batch.Count;
        logger.LogInformation("Migrated {Count} CommitParents records", commitParentsCount);
    }
    logger.LogInformation("CommitParents migration completed. Total migrated: {Count} records", commitParentsCount);

    // Перенос TestRuns (потоково, по часам)
    logger.LogInformation("Starting TestRuns migration");

    // Начинаем с текущей даты и часа
    var currentDateTime = new DateTime(2025, 04, 29, 23, 0, 0);
    // Минимальная дата (6 месяцев назад)
    var minDateTime = new DateTime(2025, 05, 19).AddMonths(-6);
    int totalTestRunsCount = 0;

    while (currentDateTime >= minDateTime)
    {
        logger.LogInformation("Processing TestRuns data for {DateTime}", currentDateTime.ToString("yyyy-MM-dd HH:00"));

        int hourlyCount = 0;
        await foreach (var batch in sourceDb.TestRuns.GetByHourAsync(currentDateTime).Batches(5000))
        {
            hourlyCount += batch.Count;
            await Retry.Action(() => targetDb.TestRuns.InsertBatchAsync(batch.ToAsyncEnumerable()), TimeSpan.FromMinutes(10));
            totalTestRunsCount += batch.Count;
            logger.LogInformation("Processed {BatchCount}/{TotalCount} TestRuns records for {DateTime}", batch.Count, hourlyCount, currentDateTime.ToString("yyyy-MM-dd HH:00"));
        }

        logger.LogInformation("Total processed {Count} TestRuns records for {DateTime}", hourlyCount, currentDateTime.ToString("yyyy-MM-dd HH:00"));

        // Если мы уже прошли минимальную дату и нет данных за текущий час, останавливаемся
        if (hourlyCount == 0 && currentDateTime < minDateTime)
        {
            logger.LogInformation("Processing completed as there is no data for {DateTime} and the minimum period has been processed",
                currentDateTime.ToString("yyyy-MM-dd HH:00"));
            break;
        }

        // Переходим к предыдущему часу
        currentDateTime = currentDateTime.AddHours(-1);
    }

    logger.LogInformation("TestRuns migration completed. Total migrated: {Count} records", totalTestRunsCount);
}
