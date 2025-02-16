using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Kontur.TestAnalytics.Reporter.Client;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Reporter.Cli;

public class JunitReporter
{
    public JunitReporter(JunitReporterOptions options)
    {
        this.options = options;
        this.log = LogProvider.Get();
    }

    public async Task DoAsync()
    {
        var (counter, runs) = CollectTestsFromReports();

        if (counter.Total == 0 && !options.NoJunit)
        {
            log.Error($"Не нашли ни одного junit отчёта или теста в них по маске {string.Join(',', options.ReportsPaths)}");
        }

        log.Debug(counter.ToString());
        await UploadTestRuns(runs);

        var jobInfo = GetFullJobInfo(counter);
        await UploadJobInfo(jobInfo);
    }

    public (TestCount counter, List<TestRun> runs) CollectTestsFromReports()
    {
        var testCountForWholeJob = new TestCount();
        var testRunsForWholeJob = new List<TestRun>();
        foreach (var reportPath in ReportPathResolver.GetReportPaths(options.ReportsPaths))
        {
            log.Info($"Start handling the report {reportPath}");

            var testRunsFromReport = CollectTestRunsFromJunit(reportPath);
            testCountForWholeJob += testRunsFromReport.counter;
            testRunsForWholeJob.AddRange(testRunsFromReport.testRuns);

            log.Debug(testRunsFromReport.counter.ToString());
        }

        return (testCountForWholeJob, testRunsForWholeJob);
    }

    private (TestCount counter, List<TestRun> testRuns) CollectTestRunsFromJunit(string reportPath)
    {
        var isReportModified = false;
        var report = XDocument.Load(reportPath);
        var root = report.Root!;

        var testRuns = new List<TestRun>();
        if (root.Name.LocalName != "testsuites")
        {
            log.Error($"File is not junit report: {reportPath}");
            return (new TestCount(), testRuns);
        }

        var testCount = new TestCount();
        var startDateTime = GetStartDateTime();
        foreach (var testCase in root.XPathSelectElements("./testsuite/testcase"))
        {
            var testAssembleName = testCase.Parent!.Attribute("name")!.Value.Replace(".dll", string.Empty);
            var className = testCase.Attribute("classname")!.Value;
            var testCaseName = testCase.Attribute("name")!.Value;
            var testId = $"{testAssembleName}: {JUnitReportHelper.RemoveDuplicatePartInClassName(className, testCaseName)}{testCaseName}";

            var failure = testCase.Element("failure");
            var skipped = testCase.Element("skipped");
            if (failure?.Value.Contains("Test history") == false)
            {
                isReportModified = true;
                failure.Value = $"{failure.Value}\n\nTest history http://singular/test-analytics/history/?id={Uri.EscapeDataString(testId)}";
            }

            var testStatus = GetStatus(skipped is not null, failure is not null);
            CalculateTestCount(testStatus, testCount);

            testRuns.Add(new TestRun
            {
                TestId = testId,
                TestResult = testStatus,
                Duration = double.TryParse(testCase.Attribute("time")!.Value, CultureInfo.InvariantCulture, out var time)
                    ? (long)TimeSpan.FromSeconds(time).TotalMilliseconds
                    : 0,
                StartDateTime = startDateTime,
            });
        }

        if (isReportModified)
        {
            SaveModifiedReport(reportPath, report);
        }

        return (testCount, testRuns);
    }

    private static FullJobInfo GetFullJobInfo(TestCount testCount)
    {
        var startDateTime = GetStartDateTime();
        var endDateTime = DateTime.Now;

        var shortJobInfo = GetJobRunInfo();
        return new FullJobInfo
        {
            JobUrl = shortJobInfo.JobUrl,
            JobId = shortJobInfo.JobId,
            JobRunId = shortJobInfo.JobRunId,
            BranchName = shortJobInfo.BranchName,
            AgentName = shortJobInfo.AgentName,
            AgentOSName = shortJobInfo.AgentOSName,
            State = GetJobStatus(),
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            Duration = (long)(endDateTime - startDateTime).TotalSeconds,
            Triggered = Environment.GetEnvironmentVariable("GITLAB_USER_EMAIL") ?? throw new InvalidOperationException("GITLAB_USER_EMAIL environment variable is not set."),
            PipelineSource = Environment.GetEnvironmentVariable("CI_PIPELINE_SOURCE") ?? throw new InvalidOperationException("CI_PIPELINE_SOURCE environment variable is not set."),
            CommitSha = Environment.GetEnvironmentVariable("CI_COMMIT_SHA") ?? throw new InvalidOperationException("CI_COMMIT_SHA environment variable is not set."),
            CommitMessage = Environment.GetEnvironmentVariable("CI_COMMIT_MESSAGE") ?? throw new InvalidOperationException("CI_COMMIT_MESSAGE environment variable is not set."),
            CommitAuthor = Environment.GetEnvironmentVariable("CI_COMMIT_AUTHOR") ?? throw new InvalidOperationException("CI_COMMIT_AUTHOR environment variable is not set."),
            ProjectId = Environment.GetEnvironmentVariable("CI_PROJECT_ID") ?? throw new InvalidOperationException("CI_PROJECT_ID environment variable is not set."),
            CustomStatusMessage = Environment.GetEnvironmentVariable("CUSTOM_STATUS_MESSAGE") ?? string.Empty,
            TotalTestsCount = testCount.Total,
            SuccessTestsCount = testCount.Success,
            FailedTestsCount = testCount.Failed,
            SkippedTestsCount = testCount.Skipped,
        };
    }

    private static DateTime GetStartDateTime() =>
        DateTime.Parse(Environment.GetEnvironmentVariable("CI_JOB_STARTED_AT")
                       ?? throw new InvalidOperationException("CI_JOB_STARTED_AT environment variable is not set."));

    private static JobStatus GetJobStatus()
    {
        var map = new Dictionary<string, JobStatus>
        {
            { "success", JobStatus.Success },
            { "failed", JobStatus.Failed },
            { "canceled", JobStatus.Canceled },
            { "timedout", JobStatus.Timeouted },
        };
        return map[Environment.GetEnvironmentVariable("CI_JOB_STATUS") ?? throw new InvalidOperationException("CI_JOB_STATUS environment variable is not set.")];
    }

    private static TestResult GetStatus(bool isSkipped, bool isFailed)
    {
        if (isSkipped)
        {
            return TestResult.Skipped;
        }

        if (isFailed)
        {
            return TestResult.Failed;
        }

        return TestResult.Success;
    }

    private static void CalculateTestCount(TestResult status, TestCount testCount)
    {
        testCount.Total++;
        if (status == TestResult.Success)
        {
            testCount.Success++;
        }

        if (status == TestResult.Failed)
        {
            testCount.Failed++;
        }

        if (status == TestResult.Skipped)
        {
            testCount.Skipped++;
        }
    }

    private void SaveModifiedReport(string reportPath, XDocument report)
    {
        if (options.DryRun)
        {
            log.Info($"Report file {reportPath} will be modified by Test Analytics");
        }
        else
        {
            report.Save(reportPath);
            log.Info($"Report file {reportPath} modified by Test Analytics");
        }
    }

    private async Task UploadTestRuns(List<TestRun> testRuns)
    {
        if (options.DryRun)
        {
            log.Info($"Test runs will be uploaded to Test History Analytics. Batch size: ({testRuns.Count})");
        }
        else
        {
            if (testRuns.Count > 0)
            {
                await TestRunsUploader.UploadAsync(GetJobRunInfo(), testRuns);
            }

            log.Info($"Test runs uploaded to Test History Analytics. Batch size: ({testRuns.Count})");
        }
    }

    private async Task UploadJobInfo(FullJobInfo jobInfo)
    {
        if (options.DryRun)
        {
            log.Info("Job Info will be uploaded to Test History Analytics");
        }
        else
        {
            await TestRunsUploader.JobInfoUploadAsync(jobInfo);
            log.Info("Job Info uploaded to Test History Analytics");
        }
    }

    public static JobRunInfo GetJobRunInfo() =>
        new ()
        {
            JobUrl = Environment.GetEnvironmentVariable("CI_JOB_URL") ?? string.Empty,
            JobId = Environment.GetEnvironmentVariable("TEAMCITY_BUILDTYPE_ID") ?? GetNormalizedJobName() ?? string.Empty,
            JobRunId = Environment.GetEnvironmentVariable("BUILD_ID") ?? Environment.GetEnvironmentVariable("CI_JOB_ID") ?? string.Empty,
            BranchName = Environment.GetEnvironmentVariable("TEAMCITY_BRANCH") ?? Environment.GetEnvironmentVariable("CI_COMMIT_BRANCH") ?? string.Empty,
            AgentName = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.GetEnvironmentVariable("HOSTNAME") ?? string.Empty,
            AgentOSName = Environment.GetEnvironmentVariable("WRAPPER_OS") ?? RuntimeInformation.OSDescription,
        };

    private static string? GetNormalizedJobName()
    {
        var jobName = Environment.GetEnvironmentVariable("CI_JOB_NAME");
        if (jobName is null)
        {
            return null;
        }

        var match = myRegex.Match(jobName);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return jobName;
    }

    private readonly JunitReporterOptions options;
    private readonly ILog log = LogProvider.Get().ForContext<JunitReporter>();
    private static Regex myRegex = new Regex(@"(.*?)([\b\s:]+((\[.*\])|(\d+[\s:\/\\]+\d+))){1,3}\s*\z", RegexOptions.Compiled);
}
