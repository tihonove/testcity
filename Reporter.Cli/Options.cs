using CommandLine;

namespace Kontur.TestAnalytics.Reporter.Cli;

public class Options
{
    [Option('d', "dry-run")]
    public bool DryRun { get; set; }
}