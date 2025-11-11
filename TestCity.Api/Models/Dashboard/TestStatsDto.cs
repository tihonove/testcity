namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class TestStatsDto
{
    public string State { get; set; } = string.Empty;
    public long Duration { get; set; }
    public DateTime StartDateTime { get; set; }
}
