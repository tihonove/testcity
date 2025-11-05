namespace TestCity.Core.Storage.DTO;

public class PipelineRunQueryResult
{
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public int TotalTestsCount { get; set; }
    public long Duration { get; set; }
    public int SuccessTestsCount { get; set; }
    public int SkippedTestsCount { get; set; }
    public int FailedTestsCount { get; set; }
    public string State { get; set; } = string.Empty; // "Success" | "Failed" | "Canceled" | "Timeouted"
    public int JobRunCount { get; set; }
    public string CustomStatusMessage { get; set; } = string.Empty;
    public string CommitMessage { get; set; } = string.Empty;
    public string CommitAuthor { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public bool HasCodeQualityReport { get; set; }
    public Tuple<string, ushort, string, string, string>[] ChangesSinceLastRunTuple { get; set; } = [];

    public List<CommitParentsChangesEntry> ChangesSinceLastRun
    {
        get
        {
            return ChangesSinceLastRunTuple
                .Select(c => new CommitParentsChangesEntry
                {
                    ParentCommitSha = c.Item1,
                    Depth = c.Item2,
                    AuthorName = c.Item3,
                    AuthorEmail = c.Item4,
                    MessagePreview = c.Item5
                })
                .ToList();
        }
        set
        {
            ChangesSinceLastRunTuple = value
                .Select(c => Tuple.Create(
                    c.ParentCommitSha,
                    c.Depth,
                    c.AuthorName,
                    c.AuthorEmail,
                    c.MessagePreview))
                .ToArray();
        }
    }    
    
    public int TotalCoveredCommitCount { get; set; }
}
