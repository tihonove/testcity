using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;

namespace Kontur.TestCity.GitLabJobsCrawler;

public record TestReportData(TestCount Counters, List<TestRun> Runs)
{
    public static TestReportData CreateEmpty()
    {
        return new TestReportData(new TestCount(), new List<TestRun>());
    }
}

public static class TestReportDataExtensions
{
    public static TestReportData Merge(this TestReportData left, TestReportData right)
    {
        var counters = left.Counters + right.Counters;
        var runs = left.Runs.Concat(right.Runs).ToList();
        return new TestReportData(counters, runs);
    }
}
