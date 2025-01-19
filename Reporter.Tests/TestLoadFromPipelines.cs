using NUnit.Framework;
using NGitLab;
using Kontur.TestAnalytics.GitLabJobsCrawler;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    [Test]
    [Explicit]
    public async Task Test01()
    {
        // const int projectId = 17358;
        var gitLabProjectIds = new[] { 182 };

        foreach (var projectId in gitLabProjectIds)
        {
            var client = new GitLabClient("https://git.skbkontur.ru", "glpat-JpY7zGgBbJqpD5Vff9qd");
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
            var jobs = await AsyncEnumerable.Take(jobsClient.GetJobsAsync(jobsQuery), 10000).ToArrayAsync();

            foreach (var job in jobs)
            {
                if (job.Artifacts != null)
                {
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    var extractor = new JUnitExtractor();
                    var r = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (r != null)
                    {
                        Console.WriteLine($"Counters: {r.Counters.Total}");
                        foreach (var run in r.Runs)
                        {
                            Console.WriteLine($"Test Run: {run.TestId}");
                        }
                        var jobInfo = GitLabHelpers.GetFullJobInfo(job, r.Counters);
                        // if (!await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId))
                        // {
                        //     await TestRunsUploader.UploadAsync(jobInfo, r.Runs);
                        //     await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                        // }
                    }
                }
            }
        }    }
}
