using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class CheckObjectsAllResponse
{
    [JsonPropertyName("objects")]
    public required ObjectOperations[] Objects { get; init; }
}
