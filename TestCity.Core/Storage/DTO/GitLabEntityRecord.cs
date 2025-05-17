namespace TestCity.Core.Storage.DTO;

public class GitLabEntityRecord
{
    public long Id { get; set; }
    public GitLabEntityType Type { get; set; }
    public string Title { get; set; } = null!;
    public long? ParentId { get; set; }
    public string ParamsJson { get; set; } = string.Empty;
}
