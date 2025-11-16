namespace TestCity.Api.Authorization;

public class CerberusSettings
{
    public required Uri Url { get; set; }

    public string ApiKey { get; set; }

    public string DefaultService { get; set; }

    public static CerberusSettings Default => CreateFromEnvironment();

    public static CerberusSettings CreateFromEnvironment()
    {
        return new CerberusSettings
        {
            Url = new Uri(
                Environment.GetEnvironmentVariable("CERBERUS_URL") ?? throw new InvalidOperationException("CERBERUS_URL is not set"),
                UriKind.Absolute),
            ApiKey = Environment.GetEnvironmentVariable("CERBERUS_API_KEY") ?? throw new InvalidOperationException("CERBERUS_API_KEY is not set"),
            DefaultService = Environment.GetEnvironmentVariable("CERBERUS_SERVICE") ?? throw new InvalidOperationException("CERBERUS_SERVICE is not set"),
        };
    }
}
