namespace TestCity.Api.Models.Dashboard;

using System.Text.Json.Serialization;

[PublicApiDTO]
[JsonDerivedType(typeof(GroupDashboardNodeDto), typeDiscriminator: "group")]
[JsonDerivedType(typeof(ProjectDashboardNodeDto), typeDiscriminator: "project")]
public abstract class DashboardNodeDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? AvatarUrl { get; set; }
    public required string Type { get; set; }
    public required string Link { get; set; }
    public required List<GroupOrProjectPathSlugItemDto> FullPathSlug { get; set; }
}
