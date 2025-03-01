using Kontur.TestAnalytics.Core;
using Kontur.TestAnalytics.Core.Clickhouse;
using Kontur.TestAnalytics.GitLabJobsCrawler;
using Kontur.TestAnalytics.GitLabJobsCrawler.Services;
using Kontur.TestAnalytics.Reporter.Client;
using NGitLab.Models;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    private static readonly ILog Log = new SynchronousConsoleLog();

    [Test]
    [Explicit]
    public async Task TestIsJobRunExists()
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        var result = await TestRunsUploader.IsJobRunIdExists("31666195");
        Assert.That(result, Is.False);
    }

    [Test]
    public void TestOutputRootGroups()
    {
        foreach (var group in GitLabProjectsService.Projects)
        {
            Log.Info($"Group ID: {group.Id}, Title: {group.Title}");
        }
    }

    [Test]
    public void TestGetAllProjects()
    {
        foreach (var project in GitLabProjectsService.GetAllProjects())
        {
            Log.Info($"Project ID: {project.Id}, Title: {project.Title}");
        }
    }

    [Test]
    [Explicit]
    public async Task Test01()
    {
        var gitlabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        // var gitLabProjectIds = GitLabProjectsService.GetAllProjects().Select(x => x.Id).Except(new[] { "17358", "19371", "182" }).Select(x => int.Parse(x)).ToList();
        foreach (var projectId in new[] { 2189 })
        {
            Log.Info($"Pulling jobs for project {projectId}");
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
            Log.Info($"Take last {jobs.Length} jobs");

            foreach (var job in jobs)
            {
                Log.Info($"Start processing job with id: {job.Id}");
                if (job.Artifacts != null)
                {
                    try
                    {
                        var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                        Log.Info($"Artifact size for job with id: {job.Id}. Size: {artifactContents.Length} bytes");
                        var extractor = new JUnitExtractor();
                        var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                        if (extractResult != null)
                        {
                            Log.Info($"Found {extractResult.Counters.Total} tests");

                            var refId = await client.BranchOrRef(projectId, job.Ref);
                            var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult.Counters, projectId.ToString());
                            if (!await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId))
                            {
                                Log.Info($"JobRunId '{jobInfo.JobRunId}' does not exist. Uploading test runs");

                                // await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                                // await TestRunsUploader.UploadAsync(jobInfo, extractResult.Runs);
                            }
                            else
                            {
                                Log.Info($"JobRunId '{jobInfo.JobRunId}' exists. Skip uploading test runs");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Warn(exception, $"Failed to read artifact for {job.Id}");
                    }
                }
            }
        }
    }
}
