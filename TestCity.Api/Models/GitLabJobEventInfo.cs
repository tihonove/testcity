namespace TestCity.Api.Models;

using System.Text.Json.Serialization;

public class GitLabJobEventInfo
{
    [JsonPropertyName("build_status")]
    public string? BuildStatus { get; set; }

    [JsonPropertyName("object_kind")]
    public string? ObjectKind { get; set; }

    [JsonPropertyName("build_id")]
    public long BuildId { get; set; }

    [JsonPropertyName("project_id")]
    public long ProjectId { get; set; }
}
