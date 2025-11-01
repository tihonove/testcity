using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

public class AuthenticationError
{
    [JsonPropertyName("errorMessages")]
    public string[]? ErrorMessages { get; init; }

    [JsonPropertyName("errorStatus")]
    public string? ErrorStatus { get; init; }

    [JsonPropertyName("errorFixUrl")]
    public string? ErrorFixUrl { get; init; }
}
