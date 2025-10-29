namespace TestCity.Core.Infrastructure;

public class OidcSettings
{
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}
