using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class Violation
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
