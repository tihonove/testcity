namespace TestCity.Core.Storage.DTO;

public class FlakyTestQueryResult
{
    public string ProjectId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public DateTime LastRunDate { get; set; }
    public ulong RunCount { get; set; }
    public ulong FailCount { get; set; }
    public ulong FlipCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double FlipRate { get; set; }
}
