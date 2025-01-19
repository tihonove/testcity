using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using NUnit.Framework;
using NGitLab;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Reflection;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestAnalytics.Reporter.Cli;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    [Test]
    public async Task Test01()
    {
        var client = new GitLabClient("https://git.skbkontur.ru", "glpat-JpY7zGgBbJqpD5Vff9qd");
        // const int projectId = 17358;
        const int projectId = 182;
        IPipelineClient pipelineClient = client.GetPipelines(projectId);
        var pipelines = pipelineClient.All.Take(30).ToArray();

        foreach (var pipeline in pipelines)
        {
            var jobs = pipelineClient.GetJobsAsync(new NGitLab.Models.PipelineJobQuery { PipelineId = pipeline.Id }).ToArray();
            foreach (var job in jobs)
            {
                if (job.Artifacts != null)
                {
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    var t = Path.Combine("/", "home", "tihonove", "workspace", "tmp", "Artifacts");
                    Directory.CreateDirectory(t);
                    string path = Path.Combine(t, $"{job.Id}_" + job.Artifacts.Filename);
                    File.WriteAllBytes(
                        path,
                        artifactContents
                    );

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
                        if (await TestRunsUploader.IsJobRunIdExists(jobInfo.JobRunId)) 
                        {
                            // await TestRunsUploader.UploadAsync(jobInfo, r.Runs);
                            // await TestRunsUploader.JobInfoUploadAsync(jobInfo);
                        }
                    }
                }
            }
        }
    }
}

public static class GitLabHelpers
{
    public static FullJobInfo GetFullJobInfo(NGitLab.Models.Job job, TestCount testCount)
    {
        var endDateTime = DateTime.Now;

        var shortJobInfo = new JobRunInfo()
        {
            JobUrl = job.WebUrl,
            JobId = job.Name,
            JobRunId = job.Id.ToString(),
            BranchName = job.Ref,
            // TODO Надо добавить в модельки NGitLab поля
            AgentName = job.Runner.Description ?? $"agent_${job.Runner.Id}",
            AgentOSName = "Unknown",
        };
        return new FullJobInfo
        {
            JobUrl = shortJobInfo.JobUrl,
            JobId = shortJobInfo.JobId,
            JobRunId = shortJobInfo.JobRunId,
            BranchName = shortJobInfo.BranchName,
            AgentName = shortJobInfo.AgentName,
            AgentOSName = shortJobInfo.AgentOSName,
            State = GetJobStatus(job.Status),
            StartDateTime = job.StartedAt,
            EndDateTime = endDateTime,
            Duration = (int)Math.Ceiling(job.QueuedDuration ?? 0),
            Triggered = job.User.Email,
            // TODO job.Pipeline.Source -- это поляна есть в api но в модель её не протащили , надо сгонять к чувакам и сделать им PR
            PipelineSource = "push",
            CommitSha = job.Commit.Id.ToString(),
            CommitMessage = job.Commit.Message,
            CommitAuthor = job.Commit.AuthorName,
            ProjectId = "Diadoc",
            CustomStatusMessage = "",
            TotalTestsCount = testCount.Total,
            SuccessTestsCount = testCount.Success,
            FailedTestsCount = testCount.Failed,
            SkippedTestsCount = testCount.Skipped
        };
    }

    private static Client.JobStatus GetJobStatus(NGitLab.JobStatus gitlabStatus)
    {
        if (gitlabStatus == NGitLab.JobStatus.Success)
        {
            return Client.JobStatus.Success;
        }
        else if (gitlabStatus == NGitLab.JobStatus.Canceled)
        {
            return Client.JobStatus.Canceled;
        }
        else if (gitlabStatus == NGitLab.JobStatus.Failed)
        {
            return Client.JobStatus.Failed;
        }
        return Client.JobStatus.Failed;
    }
}