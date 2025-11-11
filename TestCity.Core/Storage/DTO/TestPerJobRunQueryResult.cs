namespace TestCity.Core.Storage.DTO;

public class TestPerJobRunQueryResult
{
    public string JobId { get; set; } = string.Empty;
    public string JobRunId { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public long Duration { get; set; }
    public DateTime StartDateTime { get; set; }
    public string JobUrl { get; set; } = string.Empty;
    public string CustomStatusMessage { get; set; } = string.Empty;
}
