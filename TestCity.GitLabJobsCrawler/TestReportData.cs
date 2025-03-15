using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;

namespace Kontur.TestCity.GitLabJobsCrawler;

public record TestReportData(
    TestCount Counters,
    List<TestRun> Runs);
