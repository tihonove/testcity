using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace TestCity.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IOpenTelemetryBuilder ConfigureTestAnalyticsOpenTelemetry(
        this IOpenTelemetryBuilder builder,
        string project,
        string application,
        Action<MeterProviderBuilder>? configureMeter = null)
    {
        var otelEndpoint = GetOtelEndpoint();
        var otelHeaders = GetOtelHeaders();

        return builder
            .WithMetrics(x =>
            {
                configureMeter?.Invoke(x);

                x.AddOtlpExporter(c =>
                {
                    c.Endpoint = new Uri(otelEndpoint, "v1/metrics");
                    c.Protocol = OtlpExportProtocol.HttpProtobuf;
                    c.Headers = otelHeaders;
                }).ConfigureResource(
                    resourceBuilder => ConfigureResource(resourceBuilder, application, project));
            })
            .WithLogging(x => x.AddOtlpExporter(c =>
                {
                    c.Endpoint = new Uri(otelEndpoint, "v1/logs");
                    c.Protocol = OtlpExportProtocol.HttpProtobuf;
                    c.Headers = otelHeaders;
                }).ConfigureResource(
                    resourceBuilder => ConfigureResource(resourceBuilder, application, project)));
    }

    private static void ConfigureResource(ResourceBuilder resourceBuilder, string application, string project)
    {
        resourceBuilder.AddService(application, project);

        var attributes = new List<KeyValuePair<string, object>>
        {
            new ("project", project),
            new ("application", application),
        };

        var environment = Environment.GetEnvironmentVariable("OTEL_ENVIRONMENT") ?? "dev";
        attributes.Add(new KeyValuePair<string, object>("environment", environment));
        AddCustomAttributesFromEnvironment(attributes);
        resourceBuilder.AddAttributes(attributes);
    }

    private static Uri GetOtelEndpoint()
    {
        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        return new Uri(endpoint ?? "https://opentm-http.kontur.host");
    }

    private static string GetOtelHeaders()
    {
        return Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS") ?? string.Empty;
    }

    private static void AddCustomAttributesFromEnvironment(List<KeyValuePair<string, object>> attributes)
    {
        foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
        {
            var key = envVar.Key.ToString();
            if (key?.StartsWith("OTEL_ATTRIBUTE_") == true)
            {
                var attributeKey = key.Substring("OTEL_ATTRIBUTE_".Length).ToLowerInvariant();
                var attributeValue = envVar.Value?.ToString() ?? string.Empty;

                attributes.Add(new KeyValuePair<string, object>(attributeKey, attributeValue));
            }
        }
    }

    /// <summary>
    /// Проверяет, настроена ли телеметрия на основании наличия обязательных переменных окружения
    /// </summary>
    /// <returns>True если телеметрия настроена и должна быть включена</returns>
    public static bool IsOpenTelemetryEnabled()
    {
        // Проверяем наличие основного эндпоинта для телеметрии
        var endpointExists = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        // Дополнительно можно проверить специальный флаг для явного выключения телеметрии
        var explicitlyDisabled = string.Equals(
            Environment.GetEnvironmentVariable("OTEL_SDK_DISABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return endpointExists && !explicitlyDisabled;
    }
}
