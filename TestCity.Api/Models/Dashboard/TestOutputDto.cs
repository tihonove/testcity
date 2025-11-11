namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class TestOutputDto
{
    public string FailureMessage { get; set; } = string.Empty;
    public string FailureOutput { get; set; } = string.Empty;
    public string SystemOutput { get; set; } = string.Empty;
}
