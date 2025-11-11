namespace TestCity.Core.Storage.DTO;

public class TestRunForCsvQueryResult
{
    public long RowNumber { get; set; }
    public string TestId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public long Duration { get; set; }
}
