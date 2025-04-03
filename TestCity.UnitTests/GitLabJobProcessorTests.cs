using FluentAssertions;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using Kontur.TestCity.GitLabJobsCrawler;
using Microsoft.Extensions.Logging;
using NGitLab.Models;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests;

[TestFixture]
[Explicit]
public class GitLabJobProcessorTests
{
    private static readonly ILoggerFactory LoggerFactory = GlobalSetup.TestLoggerFactory;
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
        var extractor = new JUnitExtractor(GlobalSetup.TestLoggerFactory.CreateLogger<JUnitExtractor>());
        var jobProcessor = new GitLabJobProcessor(client, extractor, logger);

        var processingResult = await jobProcessor.ProcessJobAsync(projectId, jobId);

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
        var extractor = new JUnitExtractor(GlobalSetup.TestLoggerFactory.CreateLogger<JUnitExtractor>());
        var jobProcessor = new GitLabJobProcessor(client, extractor, logger);

        var processingResult = await jobProcessor.ProcessJobAsync(projectId, jobId);

        processingResult.JobInfo!.State.Should().Be(JobStatus.Failed);
    }

    [Test]
    public async Task FixMissedFormsJobs()
    {
        var projectId = 17358;
        logger.LogInformation("Starting Test01...");
        var gitlabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        logger.LogInformation("Pulling jobs for project {ProjectId}", projectId);
        var client = gitlabClientProvider.GetClient();
        var jobsClient = client.GetJobs(projectId);
        var projectInfo = await client.Projects.GetByIdAsync(projectId, new SingleProjectQuery());
        var jobsQuery = new NGitLab.Models.JobQuery
        {
            PerPage = 300,
            Scope = NGitLab.Models.JobScopeMask.All &
                ~NGitLab.Models.JobScopeMask.Canceled &
                ~NGitLab.Models.JobScopeMask.Skipped &
                ~NGitLab.Models.JobScopeMask.Pending &
                ~NGitLab.Models.JobScopeMask.Running &
                ~NGitLab.Models.JobScopeMask.Created,
        };
        var jobs = Enumerable.TakeWhile(jobsClient.GetJobsAsync(jobsQuery), x => x.CreatedAt > DateTime.Now.AddDays(-6));
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
                    bool exists = await TestRunsUploader.IsJobRunIdExists(job.Id.ToString());
                    if (exists)
                    {
                        logger.LogInformation("JobRunId '{JobRunId}' exists. Skip processing test runs", job.Id);
                        continue;
                    }
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);
                    var extractor = new JUnitExtractor(LoggerFactory.CreateLogger<JUnitExtractor>());
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
}
