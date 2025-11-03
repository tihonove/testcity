namespace TestCity.Core.Storage.DTO;

public class JobRunQueryResult
{
    public string JobId { get; set; } = string.Empty;
    public string JobRunId { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public int? TotalTestsCount { get; set; }
    public string AgentOSName { get; set; } = string.Empty;
    public long? Duration { get; set; }
    public int? SuccessTestsCount { get; set; }
    public int? SkippedTestsCount { get; set; }
    public int? FailedTestsCount { get; set; }
    public string State { get; set; } = string.Empty; // "Failed" | "Success" | "Canceled" | "Running"
    public string CustomStatusMessage { get; set; } = string.Empty;
    public string JobUrl { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public bool HasCodeQualityReport { get; set; }
    public List<CommitParentsChangesEntry> ChangesSinceLastRun { get; set; } = new();
    public int TotalCoveredCommitCount { get; set; }
}
