using System.Globalization;
using Kontur.TestAnalytics.Reporter.Client;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class JobInfoTests
{
    [Test]
    public async Task METHOD()
    {
        var startedAtFromJobEnv = "2024-10-30T10:42:19+03:00"; // тут время уже в указаном поясе (то есть по UTC = 05:42:19)
        
        var timestampFromKewebJunit = "2024-10-29T14:17:11"; // совпадает с временем ОКОНЧАНИЯ теста в логах джобы (UTC)
        var timestampFromAllFormsTests = "2024-10-29T13:44:54"; // совпадает с временем начала теста в логах джобы (UTC)
        
        var start = DateTime.Parse("2024-10-30T12:59:21");
        var end = DateTime.Parse("2024-10-30T13:34:32");
        var duration = (long)(end - start).TotalMilliseconds;
        
        var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        var commitMessage = $"wip start = {start.ToUniversalTime()}, end = {end}, utcOffset = {utcOffset}";

        await TestRunsUploader.JobInfoUploadAsync(new FullJobInfo
        {
            JobId = "LOCAL_RUN_FOR_TEST_JOB",
            JobRunId = "1",
            BranchName = "milkov-test-local-run",
            AgentName = "KE-FRM-AGENT-01",
            AgentOSName = "Windows",
            JobUrl = "https://kontur.ru",
            TotalTestsCount = 101,
            SuccessTestsCount = 99,
            FailedTestsCount = 1,
            SkippedTestsCount = 1,
            State = JobStatus.Timeouted,
            Duration = duration,
            StartDateTime = start,
            EndDateTime = end,
            Triggered = "milkov.de@skbkontur.ru",
            PipelineSource = "push",
            CommitSha = "sdf4354sdf4354sdf4354sdf4354",
            CommitMessage = commitMessage,
            CommitAuthor = "milkov <milkov@skbkontur.ru>",
            ProjectId = "12345",
            CustomStatusMessage = "Hello world!",
        });
    }
}