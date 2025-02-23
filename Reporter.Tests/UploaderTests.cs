using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

[TestFixture]
[Ignore("Загрузка в рабочую таблицу. Использовать только если ты понимаешь что делаешь")]
public class UploaderTests
{
    [Test]
    [Ignore("Загрузка в рабочую таблицу TestRuns. Использовать только если ты понимаешь что делаешь")]
    public async Task TestRunsUploadTest()
    {
        var path = Path.GetFullPath("../../junit");
        var report = new JunitReporter(new JunitReporterOptions { ReportsPaths = new[] { path + "/**" } });

        Environment.SetEnvironmentVariable("CI_JOB_STARTED_AT", DateTime.Now.ToString("O"), EnvironmentVariableTarget.Process);

        var (_, runs) = report.CollectTestsFromReports();
        var jobInfo = new JobRunInfo
        {
            JobUrl = "fakeurl",
            JobId = "Integration tests",
            PipelineId = "001",
            JobRunId = "001",
            BranchName = "milkov/additional-info-for-tha-local",
            AgentName = "runner-xwgvg2526-project-milkov-concurrent-16-57l4n2q9",
            AgentOSName = "milkov",
        };

        await TestRunsUploader.UploadAsync(jobInfo, runs);
    }

    [Test]
    [Ignore("Загрузка в рабочую таблицу JobInfo. Использовать только если ты понимаешь что делаешь")]
    public async Task JobInfoUploadTest()
    {
        // var startedAtFromJobEnv = "2024-10-30T10:42:19+03:00"; // тут время уже в указаном поясе (то есть по UTC = 05:42:19)
        // var timestampFromKewebJunit = "2024-10-29T14:17:11"; // совпадает с временем ОКОНЧАНИЯ теста в логах джобы (UTC)
        // var timestampFromAllFormsTests = "2024-10-29T13:44:54"; // совпадает с временем начала теста в логах джобы (UTC)
        var start = DateTime.Parse("2024-10-30T12:59:21");
        var end = DateTime.Parse("2024-10-30T13:34:32");
        var duration = (long)(end - start).TotalMilliseconds;

        var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        var commitMessage = $"wip start = {start.ToUniversalTime()}, end = {end}, utcOffset = {utcOffset}";

        await TestRunsUploader.JobInfoUploadAsync(new FullJobInfo
        {
            JobId = "LOCAL_RUN_FOR_TEST_JOB",
            JobRunId = "1",
            PipelineId = "1",
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
