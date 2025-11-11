namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class JobDashboardInfoDto
{
    public required string JobId { get; set; }
    public required List<JobRunDto> Runs { get; set; }
}
