namespace Kontur.TestAnalytics.Reporter.Client;

public class CommitParentsEntry
{
    public long ProjectId { get; set; }
    public string CommitSha { get; set; }
    public string ParentCommitSha { get; set; }
    public string AuthorName { get; set; }
    public string AuthorEmail { get; set; }
    public string? MessagePreview { get; set; }
    public int Depth { get; set; }
}
