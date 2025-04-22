namespace Kontur.TestCity.Core.Storage.DTO;

public enum BranchType
{
    Main,
    Side
}

public class CommitParentsEntry
{
    public long ProjectId { get; set; }
    public string CommitSha { get; set; }
    public string ParentCommitSha { get; set; }
    public string AuthorName { get; set; }
    public string AuthorEmail { get; set; }
    public string? MessagePreview { get; set; }
    public int Depth { get; set; }
    public BranchType BranchType { get; set; } = BranchType.Main;
}
