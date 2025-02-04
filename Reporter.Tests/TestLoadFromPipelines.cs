using NUnit.Framework;
using NGitLab;
using Kontur.TestAnalytics.GitLabJobsCrawler;
using Kontur.TestAnalytics.GitLabJobsCrawler.Services;
using Kontur.TestAnalytics.Reporter.Client;
using Vostok.Logging.Console;
using Vostok.Logging.Abstractions;
using NGitLab.Models;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    private static readonly ILog log = new SynchronousConsoleLog();

    [Test]
    [Explicit]
    public async Task TestIsJobRunExists()
    {
        var result = await TestRunsUploader.IsJobRunIdExists("31666195");
        Assert.IsTrue(result);
    }

    [Test]
    public void TestOutputRootGroups()
    {
        var rootGroups = GitLabProjectsService.Projects;
        foreach (var group in rootGroups)
        {
            log.Info($"Group ID: {group.Id}, Title: {group.Title}");
        }
    }

    [Test]
    public void TestGetAllProjects()
    {
        var allProjects = GitLabProjectsService.GetAllProjects();
        foreach (var project in allProjects)
        {
            log.Info($"Project ID: {project.Id}, Title: {project.Title}");
        }
    }

    [Test]
    [Explicit]
    public async Task Test01()
    {
        // var gitLabProjectIds = GitLabProjectsService.GetAllProjects().Select(x => x.Id).Except(new[] { "17358", "19371", "182" }).Select(x => int.Parse(x)).ToList();
        var gitLabProjectIds = new [] { 2189 };

        foreach (var projectId in gitLabProjectIds)
        {
            log.Info($"Pulling jobs for project {projectId}");
            var client = new GitLabClient("https://git.skbkontur.ru", "----------------");
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
                    ~NGitLab.Models.JobScopeMask.Created

            };
            var jobs = Enumerable.Take(jobsClient.GetJobsAsync(jobsQuery), 3000).ToArray();
            log.Info($"Take last {jobs.Length} jobs");

            foreach (var job in jobs)
            {
                log.Info($"Start processing job with id: {job.Id}");
                if (job.Artifacts != null)
                {
                    try
                    {
                        var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                        log.Info($"Artifact size for job with id: {job.Id}. Size: {artifactContents.Length} bytes");
                        var extractor = new JUnitExtractor();
                        var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                        if (extractResult != null)
                        {
                            log.Info($"Found {extractResult.Counters.Total} tests");

                            var refId = await client.BranchOrRef(projectId, job.Ref);
                            var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult.Counters, projectId.ToString());
                            if (!await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId))
                            {
                                log.Info($"JobRunId '{jobInfo.JobRunId}' does not exist. Uploading test runs");
                                await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                                await TestRunsUploader.UploadAsync(jobInfo, extractResult.Runs);
                            }
                            else
                            {
                                log.Info($"JobRunId '{jobInfo.JobRunId}' exists. Skip uploading test runs");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        log.Warn(exception, $"Failed to read artifact for {job.Id}");
                    }

                }
            }
        }
    }
}
