using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class PrincipalCumulativePermissions
{
    [JsonPropertyName("object")]
    public string? Object { get; init; }

    [JsonPropertyName("operations")]
    public string[]? Operations { get; init; }

    [JsonPropertyName("sources")]
    public PermissionSource[]? Sources { get; init; }
}
