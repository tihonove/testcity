namespace TestCity.Core.Storage.DTO;

public class TestListStats
{
    public ulong TotalTestsCount { get; set; }
    public ulong SuccessTestsCount { get; set; }
    public ulong SkippedTestsCount { get; set; }
    public ulong FailedTestsCount { get; set; }
}
