using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;

namespace Kontur.TestCity.GitLabJobsCrawler;

public class JUnitExtractor(ILogger<JUnitExtractor> log)
{
    public TestReportData CollectTestsFromReports(IEnumerable<string> reportPaths)
    {
        return reportPaths.Select(CollectTestRunsFromJunit).Aggregate(TestReportData.CreateEmpty(), TestReportDataExtensions.Merge);
    }

    public TestReportData CollectTestsFromStreams(IEnumerable<Stream> xmlStreams)
    {
        return xmlStreams.Select(CollectTestRunsFromJunit).Aggregate(TestReportData.CreateEmpty(), TestReportDataExtensions.Merge);
    }

    private TestReportData CollectTestRunsFromJunit(string reportPath)
    {
        using var stream = File.OpenRead(reportPath);
        var result = CollectTestRunsFromJunitInternal(stream);

        if (result.isReportModified)
        {
            SaveModifiedReport(reportPath, result.report);
        }

        return result.Item1;
    }

    public TestReportData CollectTestRunsFromJunit(Stream stream)
    {
        var result = CollectTestRunsFromJunitInternal(stream);
        return result.Item1;
    }

    private (TestReportData, XDocument report, bool isReportModified) CollectTestRunsFromJunitInternal(Stream stream)
    {
        var isReportModified = false;
        var report = XDocument.Load(stream);
        var root = report.Root!;

        var testRuns = new List<TestRun>();
        if (root.Name.LocalName != "testsuites" && root.Name.LocalName != "testsuite")
        {
            log.LogError("File is not junit report");
            return (new TestReportData(new TestCount(), testRuns), report, isReportModified);
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
            var systemOutput = testCase.Element("system-out");
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
                JUnitFailureMessage = testStatus != TestResult.Success ? failure?.Attribute("message")?.Value : null,
                JUnitFailureOutput = testStatus != TestResult.Success ? failure?.Value : null,
                JUnitSystemOutput = testStatus != TestResult.Success ? systemOutput?.Value : null,
            });
        }

        return (new TestReportData(testCount, testRuns), report, isReportModified);
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
    private readonly ILogger<JUnitExtractor> log = log;
}
