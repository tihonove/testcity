namespace Kontur.TestAnalytics.Reporter.Tests;

public record TestReportData(
    Cli.TestCount Counters,
    List<Client.TestRun> Runs
);
