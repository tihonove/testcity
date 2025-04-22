namespace Kontur.TestCity.Core.Storage.DTO;

public class CommitParentsChangesEntry
{
    public string ParentCommitSha { get; set; } = string.Empty;
    public ushort Depth { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
}
