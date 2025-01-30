using NUnit.Framework;
using NGitLab;
using Kontur.TestAnalytics.GitLabJobsCrawler;
using Kontur.TestAnalytics.Reporter.Client;
using Vostok.Logging.Console;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    [Test]
    [Explicit]
    public async Task TestIsJobRunExists()
    {
        var result = await TestRunsUploader.IsJobRunIdExists("31666195");
        Assert.IsTrue(result);
    }


    [Test]
    [Explicit]
    public async Task Test01()
    {
        var  processedJobSet = new HashSet<long>();
        var log = new ConsoleLog();
        // const int projectId = 17358;
        var gitLabProjectIds = new[] { 182 };

        foreach (var projectId in gitLabProjectIds)
        {
            log.Info($"Pulling jobs for project {projectId}");
            var client = new GitLabClient("https://git.skbkontur.ru", "INSERT YOUR TOKEN HERE");
            var jobsClient = client.GetJobs(projectId);
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
                if (processedJobSet.Contains(job.Id))
                {
                    log.Info($"Skip job with id: {job.Id}");
                    continue;
                }
                log.Info($"Start processing job with id: {job.Id}");
                if (job.Artifacts != null)
                {
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    log.Info($"Artifact size for job with id: {job.Id}. Size: {artifactContents.Length} bytes");
                    var extractor = new JUnitExtractor();
                    var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (extractResult != null)
                    {
                        var jobInfo = GitLabHelpers.GetFullJobInfo(job, job.Ref, extractResult.Counters);
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
                        processedJobSet.Add(job.Id);
                    }
                }
            }
        }



    }
}
