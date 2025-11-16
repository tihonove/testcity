using System.Text.Json.Serialization;

namespace TestCity.Cerberus.Client.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AuthSidIdentity), typeDiscriminator: "authSid")]
[JsonDerivedType(typeof(ApiKeyIdentity), typeDiscriminator: "apiKey")]
[JsonDerivedType(typeof(PortalUserIdentity), typeDiscriminator: "portalUserId")]
[JsonDerivedType(typeof(PortalApplicationIdentity), typeDiscriminator: "ApplicationId")]
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
    [JsonPropertyName("userId")]
    public required Guid UserId { get; init; }
}

public class PortalApplicationIdentity : SubjectIdentity
{
    [JsonPropertyName("appId")]
    public required Guid AppId { get; init; }
}
