namespace TestCity.Core.Storage.DTO;

public class TestRunQueryResult
{
    public string FinalState { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public double AvgDuration { get; set; }
    public long MinDuration { get; set; }
    public long MaxDuration { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string AllStates { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public ulong TotalRuns { get; set; }
    public ulong StateWeight { get; set; }
}
