using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.JobProcessing;
using TestCity.Core.JUnit;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using Microsoft.Extensions.Logging;
using NGitLab.Models;
using Xunit;
using JobScope = TestCity.Core.GitLab.Models.JobScope;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Explicits;

[Collection("Global")]
public class GitLabJobProcessorTests(ITestOutputHelper output)
{
    private readonly ILogger<GitLabJobProcessor> logger = GlobalSetup.TestLoggerFactory(output).CreateLogger<GitLabJobProcessor>();
    private readonly GitLabSettings settings = GitLabSettings.Default;

    [Fact]
    public async Task PrintJobData_ForSpecificProjectAndJob()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        var projectId = 17358;
        var jobId = 37359127;

        var gitLabClientProvider = new SkbKonturGitLabClientProvider(settings);
        var client = gitLabClientProvider.GetClient();
        var clientEx = gitLabClientProvider.GetExtendedClient();
        var extractor = new JUnitExtractor();
        var jobProcessor = new GitLabJobProcessor(client, clientEx, extractor, logger);

        var processingResult = await jobProcessor.ProcessJobAsync(projectId, jobId, null, false);

        processingResult.JobInfo!.State.Should().Be(JobStatus.Failed);
        processingResult.TestReportData!.Runs.Should().HaveCount(421);
        processingResult.JobInfo.CustomStatusMessage.Should().Be("Не прошли тесты на подхватывание ресурсов после релиза (exitCode 1)");
    }    
    
    [Fact]
    public async Task PrintJobData_ForSpecificProjectAndJob_2()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        var projectId = 17358;
        var jobId = 37342578;

        var gitLabClientProvider = new SkbKonturGitLabClientProvider(settings);
        var client = gitLabClientProvider.GetClient();
        var extractor = new JUnitExtractor();
        var clientEx = gitLabClientProvider.GetExtendedClient();
        var jobProcessor = new GitLabJobProcessor(client, clientEx, extractor, logger);

        var processingResult = await jobProcessor.ProcessJobAsync(projectId, jobId, null, false);

        processingResult.JobInfo!.State.Should().Be(JobStatus.Failed);
    }

    [Fact]
    public async Task IterateOverFiitProject()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        var projectId = 19564;
        logger.LogInformation("Starting Test01...");
        var gitlabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        logger.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var client = gitlabClientProvider.GetClient();
        var clientEx = gitlabClientProvider.GetExtendedClient();
        const JobScope scopes = JobScope.All &
                                ~JobScope.Canceled &
                                ~JobScope.Skipped &
                                ~JobScope.Pending &
                                ~JobScope.Running &
                                ~JobScope.Created;
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, scopes, perPage: 100).Take(600).ToListAsync();

        int count = 0;
        foreach (var job in jobs)
        {
            count++;
            // logger.LogInformation("Start processing job with id: {JobId}", job.Id);
            if (job.Artifacts != null)
            {
                try
                {
                    bool exists = await new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default)).JobInfo.ExistsAsync(job.Id.ToString());
                    if (exists)
                    {
                        logger.LogInformation("JobRunId '{JobRunId}' exists. Skip processing test runs", job.Id);
                        continue;
                    }
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);
                    var extractor = new JUnitExtractor();
                    var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (extractResult.TestReportData != null)
                    {
                        logger.LogInformation("Found job with artifacts. {JobRunId}", job.Id);
                        var refId = await client.BranchOrRef(projectId, job.Ref);
                        var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult, projectId.ToString());
                        if (!exists)
                        {
                            logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", jobInfo.JobRunId);
                            // await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                            // await TestRunsUploader.UploadAsync(jobInfo, extractResult.TestReportData.Runs);
                        }
                        else
                        {
                            logger.LogInformation("JobRunId '{JobRunId}' exists. Skip uploading test runs", jobInfo.JobRunId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to read artifact for {JobId}", job.Id);
                }
            }
        }
        logger.LogInformation("Processed {Count} jobs", count);
    }


    [Fact]
    public async Task FixMissedFormsJobsForPipeline()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        var projectId = 17358;
        // var pipelineId = 4071111;
        logger.LogInformation("Starting Test01...");
        var gitlabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        logger.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var client = gitlabClientProvider.GetClient();
        var clientEx = gitlabClientProvider.GetExtendedClient();
        var jobsClient = client.GetJobs(projectId);
        var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
        const JobScope scopes = JobScope.All &
                                ~JobScope.Canceled &
                                ~JobScope.Skipped &
                                ~JobScope.Pending &
                                ~JobScope.Running &
                                ~JobScope.Created;
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, scopes, perPage: 100).Take(600).ToListAsync();
        //var jobs = Enumerable.Take(jobsClient.GetJobsAsync(jobsQuery), 100);
        // logger.LogInformation("Take last {JobsLength} jobs", jobs.Length);

        int count = 0;
        foreach (var job in jobs)
        {
            count++;
            // logger.LogInformation("Start processing job with id: {JobId}", job.Id);
            if (job.Artifacts != null)
            {
                try
                {
                    bool exists = await new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default)).JobInfo.ExistsAsync(job.Id.ToString());
                    if (exists)
                    {
                        logger.LogInformation("JobRunId '{JobRunId}' exists. Skip processing test runs", job.Id);
                        continue;
                    }
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);
                    var extractor = new JUnitExtractor();
                    var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (extractResult.TestReportData != null)
                    {
                        var refId = await client.BranchOrRef(projectId, job.Ref);
                        var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult, projectId.ToString());
                        if (!exists)
                        {
                            logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", jobInfo.JobRunId);
                            // await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                            // await TestRunsUploader.UploadAsync(jobInfo, extractResult.TestReportData.Runs);
                        }
                        else
                        {
                            logger.LogInformation("JobRunId '{JobRunId}' exists. Skip uploading test runs", jobInfo.JobRunId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to read artifact for {JobId}", job.Id);
                }
            }
        }
        logger.LogInformation("Processed {Count} jobs", count);
    }

    [Fact]
    public async Task FixMissedFormsJobs()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        var projectId = 19371;
        logger.LogInformation("Starting Test01...");
        var gitlabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        logger.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var client = gitlabClientProvider.GetClient();
        var clientEx = gitlabClientProvider.GetExtendedClient();
        var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
        const JobScope scopes = JobScope.All &
                                ~JobScope.Canceled &
                                ~JobScope.Skipped &
                                ~JobScope.Pending &
                                ~JobScope.Running &
                                ~JobScope.Created;
        var jobs = await clientEx.GetAllProjectJobsAsync(projectId, scopes, perPage: 100).TakeWhile(x => x.CreatedAt > DateTime.Now.AddDays(-6)).ToListAsync();
        //var jobs = Enumerable.Take(jobsClient.GetJobsAsync(jobsQuery), 100);
        // logger.LogInformation("Take last {JobsLength} jobs", jobs.Length);
    
        var database = new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default));
        int count = 0;
        foreach (var job in jobs)
        {
            count++;
            // logger.LogInformation("Start processing job with id: {JobId}", job.Id);
            if (job.Artifacts != null)
            {
                try
                {
                    bool exists = await database.JobInfo.ExistsAsync(job.Id.ToString());
                    if (exists)
                    {
                        logger.LogInformation("JobRunId '{JobRunId}' exists. Skip processing test runs", job.Id);
                        continue;
                    }
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);
                    var extractor = new JUnitExtractor();
                    var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (extractResult.TestReportData != null)
                    {
                        var refId = await client.BranchOrRef(projectId, job.Ref);
                        var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult, projectId.ToString());
                        if (!exists)
                        {
                            logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", jobInfo.JobRunId);
                            await database.JobInfo.InsertAsync(jobInfo);
                            await database.TestRuns.InsertBatchAsync(jobInfo, extractResult.TestReportData.Runs);
                        }
                        else
                        {
                            logger.LogInformation("JobRunId '{JobRunId}' exists. Skip uploading test runs", jobInfo.JobRunId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to read artifact for {JobId}", job.Id);
                }
            }
        }
        logger.LogInformation("Processed {Count} jobs", count);
    }
}
