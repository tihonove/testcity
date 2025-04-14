namespace Kontur.TestCity.Core.JUnit;

public static class TestReportDataExtensions
{
    public static TestReportData Merge(this TestReportData left, TestReportData right)
    {
        var counters = left.Counters + right.Counters;
        var runs = left.Runs.Concat(right.Runs).ToList();
        return new TestReportData(counters, runs);
    }
}
