namespace Kontur.TestAnalytics.Reporter.Client;

public record TestRun
{
    public required string TestId { get; init; }
    public required TestResult TestResult { get; init; }
    public required long Duration { get; init; }
    public required DateTime StartDateTime { get; init; }
}
