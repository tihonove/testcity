using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class PermissionSource
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
