using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class CheckObjectsByHierarchyLevelRequest
{
    [JsonPropertyName("service")]
    public required string Service { get; init; }

    [JsonPropertyName("subjectIdentity")]
    public required SubjectIdentity SubjectIdentity { get; init; }

    [JsonPropertyName("operations")]
    public string[]? Operations { get; init; }

    [JsonPropertyName("hierarchyLevel")]
    public int HierarchyLevel { get; init; } = 1;
}
