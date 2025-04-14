using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.JUnit;

public record TestReportData(TestCount Counters, List<TestRun> Runs)
{
    public static TestReportData CreateEmpty()
    {
        return new TestReportData(new TestCount(), new List<TestRun>());
    }
}
