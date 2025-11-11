namespace TestCity.Api.Models.Dashboard;

[PublicApiDTO]
public class GroupOrProjectPathSlugItemDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? AvatarUrl { get; set; }
}
