namespace Kontur.TestAnalytics.Reporter.Cli;

public class JunitReporterOptions
{
    public required IEnumerable<string> ReportsPaths { get; set; }

    public bool NoJunit { get; set; }

    public bool DryRun { get; set; }
}
