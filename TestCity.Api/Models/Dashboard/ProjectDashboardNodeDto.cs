namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class ProjectDashboardNodeDto : DashboardNodeDto
{
    public required string GitLabLink { get; set; }
    public required List<JobDashboardInfoDto> Jobs { get; set; }
}
