using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Kontur.TestAnalytics.Reporter.Client;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Reporter.Cli;

public class JunitReporter
{
    public JunitReporter(JunitRepoterOptions options, ILog log)
    {
        this.options = options;
        this.log = log;
    }

    public Task DoAsync()
    {
        var startDateTime = DateTime.Now;
        var reportPaths = ReportPathResolver.GetReportPaths(options.ReportsPaths);

        var testRunLines = reportPaths.SelectMany(p => HandleReport(p, startDateTime));

        return options.DryRun ? Task.FromResult(testRunLines.ToList()) : TestRunsUploader.UploadAsync(GetJobRunInfo(), testRunLines);
    }

    private IEnumerable<TestRun> HandleReport(string reportPath, DateTime startDateTime)
    {
        log.Info("Start habdle report {Path}", reportPath);
        var report = XDocument.Load(reportPath);
        var isReportModified = false;
        var root = report.Root!;
        
        if (root.Name.LocalName != "testsuites")
        {
            log.Error("File is not junit report: {Path}", reportPath);
            yield break;
        }

        foreach (var testCase in root.Elements("testsuite").Select(e => e.Element("testcase")))
        {
            var testSuite = testCase!.Parent!;
            var testId = $"{testSuite.Attribute("name")!.Value}: {testCase.Attribute("classname")!.Value}{testCase.Attribute("name")!.Value}";
            _ = double.TryParse(testCase.Attribute("time")!.Value, CultureInfo.InvariantCulture, out var time);

            var failure = testCase.Element("failure");
            if (failure is not null && !failure.Value.Contains("Test history"))
            {
                isReportModified = true;
                failure.Value = $"{failure.Value}\n\nTest history http://singular/test-analytics/history/?id={Uri.EscapeDataString(testId)}";
            }

            yield return new TestRun
            {
                TestId = testId,
                TestResult = GetTestCaseResult(testCase.Element("skipped") is not null, failure is not null),
                Duration = TimeSpan.FromSeconds(time).Ticks,
                StartDateTime = DateTime.TryParse(testSuite.Attribute("timestamp")?.Value, out var timeStamp)
                    ? timeStamp
                    : startDateTime
            };
        }

        if (options.DryRun || !isReportModified)
            yield break;

        log.Info("Modify report {File} for Test Analytics", reportPath);
        report.Save(reportPath);
    }

    private static TestResult GetTestCaseResult(bool isSkipped, bool isFailed)
    {
        if (isSkipped)
            return TestResult.Skipped;

        if (isFailed)
            return TestResult.Failed;

        return TestResult.Success;
    }

    private static JobRunInfo GetJobRunInfo() =>
        new()
        {
            JobUrl = Environment.GetEnvironmentVariable("CI_JOB_URL") ?? string.Empty,
            JobId = Environment.GetEnvironmentVariable("TEAMCITY_BUILDTYPE_ID") ?? Environment.GetEnvironmentVariable("CI_JOB_NAME") ?? string.Empty,
            JobRunId = Environment.GetEnvironmentVariable("BUILD_ID") ?? Environment.GetEnvironmentVariable("CI_JOB_ID") ?? string.Empty,
            BranchName = Environment.GetEnvironmentVariable("TEAMCITY_BRANCH") ?? Environment.GetEnvironmentVariable("CI_COMMIT_BRANCH") ?? string.Empty,
            AgentName = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.GetEnvironmentVariable("HOSTNAME") ?? string.Empty,
            AgentOSName = Environment.GetEnvironmentVariable("WRAPPER_OS") ?? RuntimeInformation.OSDescription
        };

    private readonly JunitRepoterOptions options;
    private readonly ILog log;
}