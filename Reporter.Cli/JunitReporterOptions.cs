using CommandLine;

namespace Kontur.TestAnalytics.Reporter.Cli;

[Verb("junit-report")]
public class JunitReporterOptions
{
    [Option('f', "reportsPaths", Required = true)]
    public required IEnumerable<string> ReportsPaths { get; set; }

    [Option("noJunit")]
    public bool NoJunit { get; set; }

    [Option('d', "dry-run")]
    public bool DryRun { get; set; }
}
