namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class TestListStatsDto
{
    public ulong TotalTestsCount { get; set; }
    public ulong SuccessTestsCount { get; set; }
    public ulong SkippedTestsCount { get; set; }
    public ulong FailedTestsCount { get; set; }
}
