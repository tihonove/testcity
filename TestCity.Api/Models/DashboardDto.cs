namespace TestCity.Api.Models;

using System.Text.Json.Serialization;

public class GroupOrProjectPathSlugItem
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? AvatarUrl { get; set; }
}

public class JobDashboardInfoDto
{
    public required string JobId { get; set; }
    public required List<TestCity.Core.Storage.DTO.JobRunQueryResult> Runs { get; set; }
}

[JsonDerivedType(typeof(GroupDashboardNodeDto), typeDiscriminator: "group")]
[JsonDerivedType(typeof(ProjectDashboardNodeDto), typeDiscriminator: "project")]
public abstract class DashboardNodeDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? AvatarUrl { get; set; }
    public required string Type { get; set; }
    public required string Link { get; set; }
    public required List<GroupOrProjectPathSlugItem> FullPathSlug { get; set; }
}

public class GroupDashboardNodeDto : DashboardNodeDto
{
    public required List<DashboardNodeDto> Children { get; set; }
}

public class ProjectDashboardNodeDto : DashboardNodeDto
{
    public required string GitLabLink { get; set; }
    public required List<JobDashboardInfoDto> Jobs { get; set; }
}
