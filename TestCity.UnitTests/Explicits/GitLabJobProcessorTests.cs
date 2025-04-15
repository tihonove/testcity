using FluentAssertions;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.JobProcessing;
using Kontur.TestCity.Core.JUnit;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Storage.DTO;
using Kontur.TestCity.GitLabJobsCrawler;
using Microsoft.Extensions.Logging;
using NGitLab.Models;
using NUnit.Framework;
using JobScope = Kontur.TestCity.Core.GitLab.Models.JobScope;

namespace Kontur.TestCity.UnitTests.Explicits;

[TestFixture]
[Explicit]
public class GitLabJobProcessorTests
{
    private ILogger<GitLabJobProcessor> logger;
    private GitLabSettings settings;

    [OneTimeSetUp]
    public void Setup()
    {
        logger = GlobalSetup.TestLoggerFactory.CreateLogger<GitLabJobProcessor>();
        settings = GitLabSettings.Default;
    }

    [Test]
    public async Task PrintJobData_ForSpecificProjectAndJob()
    {
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
    
    [Test]
    public async Task PrintJobData_ForSpecificProjectAndJob_2()
    {
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

    [Test]
    public async Task IterateOverFiitProject()
    {
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
                    bool exists = await new TestCityDatabase(new ConnectionFactory()).JobInfo.ExistsAsync(job.Id.ToString());
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


    [Test]
    public async Task FixMissedFormsJobsForPipeline()
    {
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
                    bool exists = await new TestCityDatabase(new ConnectionFactory()).JobInfo.ExistsAsync(job.Id.ToString());
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

    [Test]
    public async Task FixMissedFormsJobs()
    {
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
    
        var database = new TestCityDatabase(new ConnectionFactory());
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
