using CommandLine;

namespace Kontur.TestAnalytics.Reporter.Cli;

[Verb("junit-report")]
public class JunitRepoterOptions : Options
{
    [Option('f', "reportsPaths", Required = true)]
    public required IEnumerable<string> ReportsPaths { get; set; }
}