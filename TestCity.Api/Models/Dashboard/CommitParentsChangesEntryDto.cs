namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class CommitParentsChangesEntryDto
{
    public required string ParentCommitSha { get; set; }
    public required ushort Depth { get; set; }
    public required string AuthorName { get; set; }
    public required string AuthorEmail { get; set; }
    public required string MessagePreview { get; set; }
}
