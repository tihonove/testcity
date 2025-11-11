namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class JobRunDto
{
    public required string JobId { get; set; }
    public required string JobRunId { get; set; }
    public required string BranchName { get; set; }
    public required string AgentName { get; set; }
    public required DateTime StartDateTime { get; set; }
    public int? TotalTestsCount { get; set; }
    public required string AgentOSName { get; set; }
    public long? Duration { get; set; }
    public int? SuccessTestsCount { get; set; }
    public int? SkippedTestsCount { get; set; }
    public int? FailedTestsCount { get; set; }
    public required string State { get; set; } // "Failed" | "Success" | "Canceled" | "Running"
    public required string CustomStatusMessage { get; set; }
    public required string JobUrl { get; set; }
    public required string ProjectId { get; set; }
    public bool HasCodeQualityReport { get; set; }
    public required List<CommitParentsChangesEntryDto> ChangesSinceLastRun { get; set; }
    public int TotalCoveredCommitCount { get; set; }
}
