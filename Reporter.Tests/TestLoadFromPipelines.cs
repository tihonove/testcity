using Kontur.TestAnalytics.Core;
using Kontur.TestAnalytics.Core.Clickhouse;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.GitLabJobsCrawler;
using Microsoft.Extensions.Logging;
using NGitLab.Models;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
    });

    private static readonly ILogger<TestsLoadFromGitlab> Logger = LoggerFactory.CreateLogger<TestsLoadFromGitlab>();

    [Test]
    [Explicit]
    public async Task TestIsJobRunExists()
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        var result = await TestRunsUploader.IsJobRunIdExists("31666195");
        Logger.LogInformation("JobRunIdExists result: {Result}", result);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TestOutputRootGroups()
    {
        foreach (var group in GitLabProjectsService.Projects)
        {
            Logger.LogInformation("Group ID: {GroupId}, Title: {Title}", group.Id, group.Title);
        }
    }

    [Test]
    public void TestGetAllProjects()
    {
        Logger.LogInformation("Starting TestGetAllProjects...");
        foreach (var project in GitLabProjectsService.GetAllProjects())
        {
            Logger.LogInformation("Project ID: {ProjectId}, Title: {Title}", project.Id, project.Title);
        }
    }

    [Test]
    [Explicit]
    public async Task Test01()
    {
        Logger.LogInformation("Starting Test01...");
        var gitlabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        foreach (var projectId in new[] { 2189 })
        {
            Logger.LogInformation("Pulling jobs for project {ProjectId}", projectId);
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
            var jobs = Enumerable.Take(jobsClient.GetJobsAsync(jobsQuery), 3000).ToArray();
            Logger.LogInformation("Take last {JobsLength} jobs", jobs.Length);

            foreach (var job in jobs)
            {
                Logger.LogInformation("Start processing job with id: {JobId}", job.Id);
                if (job.Artifacts != null)
                {
                    try
                    {
                        var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                        Logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);
                        var extractor = new JUnitExtractor(LoggerFactory.CreateLogger<JUnitExtractor>());
                        var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                        if (extractResult.TestReportData != null)
                        {
                            Logger.LogInformation("Found {TotalTests} tests", extractResult.TestReportData.Counters.Total);

                            var refId = await client.BranchOrRef(projectId, job.Ref);
                            var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult, projectId.ToString());
                            if (!await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId))
                            {
                                Logger.LogInformation("JobRunId '{JobRunId}' does not exist. Uploading test runs", jobInfo.JobRunId);

                                // await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                                // await TestRunsUploader.UploadAsync(jobInfo, extractResult.Runs);
                            }
                            else
                            {
                                Logger.LogInformation("JobRunId '{JobRunId}' exists. Skip uploading test runs", jobInfo.JobRunId);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.LogWarning(exception, "Failed to read artifact for {JobId}", job.Id);
                    }
                }
            }
        }
    }
}
