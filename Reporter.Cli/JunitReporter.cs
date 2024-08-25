using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Kontur.TestAnalytics.Reporter.Client;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Reporter.Cli;

public class JunitReporter
{
    public JunitReporter(JunitReporterOptions options, ILog log)
    {
        this.options = options;
        this.log = log;
    }

    public async Task DoAsync()
    {
        var reportPaths = ReportPathResolver.GetReportPaths(options.ReportsPaths);

        foreach (var reportPath in reportPaths)
        {
            var testRunLines = HandleReport(reportPath);
            await UploadTestRuns(testRunLines);
        }
    }

    private IEnumerable<TestRun> HandleReport(string reportPath)
    {
        log.Info($"Start handling the report {reportPath}");
        
        var startDateTime = DateTime.Now;
        var report = XDocument.Load(reportPath);
        var isReportModified = false;
        var root = report.Root!;
        
        if (root.Name.LocalName != "testsuites")
        {
            log.Error($"File is not junit report: {reportPath}");
            yield break;
        }

        foreach (var testCase in root.XPathSelectElements("./testsuite/testcase"))
        {
            var testSuite = testCase.Parent!;
            var testId = $"{testSuite.Attribute("name")!.Value.Replace(".dll", "")}: " +
                         $"{testCase.Attribute("classname")!.Value}.{testCase.Attribute("name")!.Value}";

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
                Duration = double.TryParse(testCase.Attribute("time")!.Value, CultureInfo.InvariantCulture, out var time) 
                    ? TimeSpan.FromSeconds(time).Ticks 
                    : 0,
                StartDateTime = DateTime.TryParse(testSuite.Attribute("timestamp")?.Value, out var timeStamp)
                    ? timeStamp
                    : startDateTime
            };
        }

        if (options.DryRun)
        {
            log.Info($"Report file {reportPath} will be modified by Test Analytics");
            yield break;
        }

        if (isReportModified)
        {
            report.Save(reportPath);
            log.Info($"Report file {reportPath} modified by Test Analytics");
        }
    }

    private static TestResult GetTestCaseResult(bool isSkipped, bool isFailed)
    {
        if (isSkipped)
            return TestResult.Skipped;

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (isFailed)
            return TestResult.Failed;

        return TestResult.Success;
    }
    
    private async Task UploadTestRuns(IEnumerable<TestRun> testRunLines)
    {
        if (options.DryRun)
        {
            var runLines = testRunLines.ToList();
            log.Info($"Test runs will be uploaded to Test History Analytics. Batch size: ({runLines.Count})");
            await Task.FromResult(runLines);
        }
        else
            await TestRunsUploader.UploadAsync(GetJobRunInfo(), testRunLines);
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

    private readonly JunitReporterOptions options;
    private readonly ILog log;
}