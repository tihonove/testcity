using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class CheckObjectsByNameRequest
{
    [JsonPropertyName("service")]
    public required string Service { get; init; }

    [JsonPropertyName("subjectIdentity")]
    public required SubjectIdentity SubjectIdentity { get; init; }

    [JsonPropertyName("objects")]
    public required string[] Objects { get; init; }

    [JsonPropertyName("operations")]
    public string[]? Operations { get; init; }

    [JsonPropertyName("needPermissionsDetails")]
    public bool? NeedPermissionsDetails { get; init; }
}
