using TestCity.Core.Infrastructure;

namespace TestCity.Api.Authorization;

public class AuthorizationSettings
{
    public required AuthorizationType Type { get; set; }
    public OidcSettings? Oidc { get; set; }
    public CerberusSettings? Cerberus { get; set; }

    public static AuthorizationSettings Default
    {
        get
        {
            var result = new AuthorizationSettings()
            {
                Type = Enum.TryParse<AuthorizationType>(Environment.GetEnvironmentVariable("AUTHORIZATION_TYPE"), true, out var authType)
                ? authType
                : AuthorizationType.Fake
            };
            if (result.Type == AuthorizationType.OpenIdConnect || result.Type == AuthorizationType.OpenIdConnectWithCerberus)
            {
                result.Oidc = new OidcSettings
                {
                    Authority = Environment.GetEnvironmentVariable("OIDC_AUTHORITY") ?? throw new InvalidOperationException("OIDC_AUTHORITY is not set"),
                    ClientId = Environment.GetEnvironmentVariable("OIDC_CLIENT_ID") ?? throw new InvalidOperationException("OIDC_CLIENT_ID is not set"),
                    ClientSecret = Environment.GetEnvironmentVariable("OIDC_CLIENT_SECRET") ?? throw new InvalidOperationException("OIDC_CLIENT_SECRET is not set"),
                };
            }
            if (result.Type == AuthorizationType.OpenIdConnectWithCerberus)
            {
                result.Cerberus = CerberusSettings.CreateFromEnvironment();
            }
            return result;
        }
    }
}
