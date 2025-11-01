using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class CheckObjectsResponse
{
    [JsonPropertyName("objects")]
    public required ObjectOperations[] Objects { get; init; }

    [JsonPropertyName("permissionsDetails")]
    public PrincipalCumulativePermissions[]? PermissionsDetails { get; init; }
}
