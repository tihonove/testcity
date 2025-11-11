namespace TestCity.Core.Storage.DTO;

public class JobInfoQueryResult
{
    public string JobId { get; set; } = string.Empty;
    public string JobRunId { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int TotalTestsCount { get; set; }
    public string AgentOSName { get; set; } = string.Empty;
    public long Duration { get; set; }
    public int SuccessTestsCount { get; set; }
    public int SkippedTestsCount { get; set; }
    public int FailedTestsCount { get; set; }
    public string State { get; set; } = string.Empty;
    public string CustomStatusMessage { get; set; } = string.Empty;
    public string JobUrl { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string? PipelineSource { get; set; }
    public string? Triggered { get; set; }
    public bool HasCodeQualityReport { get; set; }
    public Tuple<string, ushort, string, string, string>[] ChangesSinceLastRun { get; set; } = [];
    public int TotalCoveredCommitCount { get; set; }
    public string? PipelineId { get; set; }
    public string? CommitSha { get; set; }
    public string? CommitMessage { get; set; }
    public string? CommitAuthor { get; set; }
}
