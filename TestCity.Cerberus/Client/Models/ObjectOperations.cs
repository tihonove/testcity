using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class ObjectOperations
{
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    [JsonPropertyName("operations")]
    public string[]? Operations { get; init; }
}
