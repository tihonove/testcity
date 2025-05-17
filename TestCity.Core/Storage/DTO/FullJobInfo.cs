namespace TestCity.Core.Storage.DTO;

public class FullJobInfo : JobRunInfo
{
    public required JobStatus State { get; init; }

    public required long Duration { get; init; }

    public required DateTime StartDateTime { get; init; }

    public required DateTime EndDateTime { get; init; }

    public required string? Triggered { get; init; }

    public required string? PipelineSource { get; init; }

    public required string? CommitSha { get; init; }

    public required string? CommitMessage { get; init; }

    public required string? CommitAuthor { get; init; }

    public int TotalTestsCount { get; set; }

    public int SuccessTestsCount { get; set; }

    public int FailedTestsCount { get; set; }

    public int SkippedTestsCount { get; set; }

    public string? CustomStatusMessage { get; set; }

    public bool HasCodeQualityReport { get; set; }

    public List<CommitParentsChangesEntry> ChangesSinceLastRun { get; set; } = new();
}
