using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AuthSidIdentity), typeDiscriminator: "authSid")]
[JsonDerivedType(typeof(ApiKeyIdentity), typeDiscriminator: "apiKey")]
[JsonDerivedType(typeof(PortalUserIdentity), typeDiscriminator: "portalUser")]
[JsonDerivedType(typeof(PortalApplicationIdentity), typeDiscriminator: "portalApplication")]
public abstract class SubjectIdentity
{
}

public class AuthSidIdentity : SubjectIdentity
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
}

public class ApiKeyIdentity : SubjectIdentity
{
    [JsonPropertyName("apiKey")]
    public required string ApiKey { get; init; }
}

public class PortalUserIdentity : SubjectIdentity
{
    [JsonPropertyName("portalUserId")]
    public required Guid PortalUserId { get; init; }
}

public class PortalApplicationIdentity : SubjectIdentity
{
    [JsonPropertyName("portalApplicationId")]
    public required Guid PortalApplicationId { get; init; }
}
