namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class TestPerJobRunDto
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
