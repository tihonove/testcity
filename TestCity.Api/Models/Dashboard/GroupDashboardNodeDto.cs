namespace TestCity.Api.Models.Dashboard;

public class GroupDashboardNodeDto : DashboardNodeDto
{
    public required List<DashboardNodeDto> Children { get; set; }
}
