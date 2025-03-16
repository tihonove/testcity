using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;

namespace Kontur.TestCity.GitLabJobsCrawler;

public class JUnitExtractor
{
    public JUnitExtractor(ILogger<JUnitExtractor> log)
    {
        this.log = log;
    }

    public (TestCount counter, List<TestRun> runs) CollectTestsFromReports(IEnumerable<string> reportPaths)
    {
        var testCountForWholeJob = new TestCount();
        var testRunsForWholeJob = new List<TestRun>();
        foreach (var reportPath in reportPaths)
        {
            log.LogInformation("Start handling the report {ReportPath}", reportPath);

            var testRunsFromReport = CollectTestRunsFromJunit(reportPath);
            testCountForWholeJob += testRunsFromReport.counter;
            testRunsForWholeJob.AddRange(testRunsFromReport.testRuns);

            log.LogDebug(testRunsFromReport.counter.ToString());
        }

        return (testCountForWholeJob, testRunsForWholeJob);
    }

    private (TestCount counter, List<TestRun> testRuns) CollectTestRunsFromJunit(string reportPath)
    {
        var isReportModified = false;
        var report = XDocument.Load(reportPath);
        var root = report.Root!;

        var testRuns = new List<TestRun>();
        if (root.Name.LocalName != "testsuites" && root.Name.LocalName != "testsuite")
        {
            log.LogError($"File is not junit report: {reportPath}");
            return (new TestCount(), testRuns);
        }

        var testCount = new TestCount();
        foreach (var testCase in root.XPathSelectElements("//testsuite//testcase"))
        {
            var startDateTime = DateTimeOffset.Parse(testCase.Parent!.Attribute("timestamp")!.Value);
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
                StartDateTime = startDateTime.DateTime,
            });
        }

        if (isReportModified)
        {
            SaveModifiedReport(reportPath, report);
        }

        return (testCount, testRuns);
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
        if (doNotModifyReports)
        {
            log.LogInformation("Report file {Path} will be modified by Test Analytics", reportPath);
        }
        else
        {
            report.Save(reportPath);
            log.LogInformation("Report file {Path} modified by Test Analytics", reportPath);
        }
    }

    private readonly bool doNotModifyReports = true;
    private ILogger<JUnitExtractor> log;
}
