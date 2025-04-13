using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;
using Microsoft.Extensions.Logging;

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
        return CollectTestRunsFromJunitInternal(stream);
    }

    public TestReportData CollectTestRunsFromJunit(Stream stream)
    {
        return CollectTestRunsFromJunitInternal(stream);
    }

    private TestReportData CollectTestRunsFromJunitInternal(Stream stream)
    {
        var testRuns = new List<TestRun>();
        var testCount = new TestCount();

        using var reader = XmlReader.Create(stream);

        string? currentTestSuiteName = null;
        DateTimeOffset? currentTestSuiteTimestamp = null;

        string? testCaseClassName = null;
        string? testCaseName = null;
        double? testCaseTime = null;

        string? failureMessage = null;
        string? failureOutput = null;
        string? systemOutput = null;
        bool hasSkipped = false;
        bool hasFailure = false;

        string currentElement = string.Empty;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                currentElement = reader.Name;

                if (reader.Name == "testsuite")
                {
                    currentTestSuiteName = reader.GetAttribute("name");
                    var timestampStr = reader.GetAttribute("timestamp");
                    if (timestampStr != null)
                    {
                        currentTestSuiteTimestamp = DateTimeOffset.Parse(timestampStr);
                    }
                }
                else if (reader.Name == "testcase")
                {
                    // Начало нового testcase, сбросим значения
                    testCaseClassName = reader.GetAttribute("classname");
                    testCaseName = reader.GetAttribute("name");
                    var timeStr = reader.GetAttribute("time");
                    testCaseTime = timeStr != null ? double.Parse(timeStr, CultureInfo.InvariantCulture) : null;

                    failureMessage = null;
                    failureOutput = null;
                    systemOutput = null;
                    hasSkipped = false;
                    hasFailure = false;

                    // Проверяем, является ли элемент пустым (самозакрывающимся)
                    if (reader.IsEmptyElement)
                    {
                        // Если элемент пустой, сразу обрабатываем его как завершенный
                        ProcessTestCase(testCaseClassName, testCaseName, testCaseTime, currentTestSuiteName, 
                            currentTestSuiteTimestamp, hasSkipped, hasFailure, failureMessage, failureOutput, 
                            systemOutput, testRuns, testCount);
                    }
                }
                else if (reader.Name == "failure")
                {
                    hasFailure = true;
                    failureMessage = reader.GetAttribute("message");
                    // Содержимое failure будем читать как текст
                }
                else if (reader.Name == "skipped")
                {
                    hasSkipped = true;
                }
                else if (reader.Name == "system-out")
                {
                    // Содержимое system-out будем читать как текст
                }
            }
            else if (reader.NodeType == XmlNodeType.Text)
            {
                if (currentElement == "failure")
                {
                    failureOutput = reader.Value;
                }
                else if (currentElement == "system-out")
                {
                    systemOutput = reader.Value;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "testcase" && !reader.IsEmptyElement)
                {
                    ProcessTestCase(testCaseClassName, testCaseName, testCaseTime, currentTestSuiteName, 
                        currentTestSuiteTimestamp, hasSkipped, hasFailure, failureMessage, failureOutput, 
                        systemOutput, testRuns, testCount);
                }

                if (reader.Name == currentElement)
                {
                    currentElement = string.Empty;
                }
            }
        }

        return new TestReportData(testCount, testRuns);
    }

    private void ProcessTestCase(string? testCaseClassName, string? testCaseName, double? testCaseTime, 
        string? currentTestSuiteName, DateTimeOffset? currentTestSuiteTimestamp, 
        bool hasSkipped, bool hasFailure, string? failureMessage, string? failureOutput, string? systemOutput,
        List<TestRun> testRuns, TestCount testCount)
    {
        if (testCaseClassName != null && testCaseName != null && currentTestSuiteName != null && currentTestSuiteTimestamp.HasValue)
        {
            var testAssembleName = currentTestSuiteName.Replace(".dll", string.Empty);
            var testId = $"{testAssembleName}: {JUnitReportHelper.RemoveDuplicatePartInClassName(testCaseClassName, testCaseName)}{testCaseName}";

            var testStatus = GetStatus(hasSkipped, hasFailure);
            CalculateTestCount(testStatus, testCount);

            testRuns.Add(new TestRun
            {
                TestId = testId,
                TestResult = testStatus,
                Duration = testCaseTime.HasValue ? (long)TimeSpan.FromSeconds(testCaseTime.Value).TotalMilliseconds : 0,
                StartDateTime = currentTestSuiteTimestamp.Value.DateTime,
                JUnitFailureMessage = testStatus != TestResult.Success ? failureMessage : null,
                JUnitFailureOutput = testStatus != TestResult.Success ? failureOutput : null,
                JUnitSystemOutput = testStatus != TestResult.Success ? systemOutput : null,
            });
        }
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
    private readonly ILogger<JUnitExtractor> log = log;
}
