namespace Kontur.TestAnalytics.Reporter.Client;

public class JobRunInfo
{
    public required string JobId { get; init; }

    public required string JobRunId { get; init; }

    public required string BranchName { get; init; }

    public required string AgentName { get; init; }

    public required string AgentOSName { get; init; }

    public required string JobUrl { get; init; }
}
