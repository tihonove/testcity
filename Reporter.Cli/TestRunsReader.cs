using System.Text;
using Kontur.TestAnalytics.Reporter.Client;
using Microsoft.VisualBasic.FileIO;

namespace Kontur.TestAnalytics.Reporter.Cli;

public static class TestRunsReader
{
    public static async IAsyncEnumerable<TestRun> ReadFromTeamcityTestReport(
        string filePath,
        DateTime? explicitStartDateTime = null)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var parser = new TextFieldParser(fileStream, Encoding.UTF8);
        parser.Delimiters = new[] { "," };
        parser.ReadFields();
        while (true)
        {
            var parts = parser.ReadFields();
            if (parts == null)
            {
                break;
            }

            yield return new TestRun
            {
                TestId = parts[1],
                TestResult = FromTeamcityStatusToTestResult(parts[2]),
                Duration = long.Parse(parts[3]),
                StartDateTime = explicitStartDateTime ?? DateTime.Now,
            };
        }
    }

    private static TestResult FromTeamcityStatusToTestResult(string teamcityStatus)
    {
        if (teamcityStatus == "OK")
        {
            return TestResult.Success;
        }

        if (teamcityStatus == "Ignored")
        {
            return TestResult.Skipped;
        }

        if (teamcityStatus == "Failure")
        {
            return TestResult.Failed;
        }

        throw new Exception($"Unknown teamcity result {teamcityStatus}");
    }
}
