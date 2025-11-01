using Microsoft.Extensions.Logging;
using TestCity.Api.Authorization;
using TestCity.Cerberus.Client;
using TestCity.Cerberus.Client.Models;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Cerberus;

[Collection("Global")]
public class CerberusAuthorizationManualTests(ITestOutputHelper output)
{
    private readonly ILogger logger = GlobalSetup.TestLoggerFactory(output).CreateLogger(nameof(CerberusAuthorizationManualTests));

    [Fact(Skip = "Требуется подключение к рабочему Cerberus API")]
    public async Task TestAuthorize()
    {
        var settings = CerberusSettings.Default;
        var cerberusClient = new CerberusClient(settings.Url, settings.ApiKey, logger);

        var request = new CheckObjectsByHierarchyLevelRequest
        {
            Service = settings.DefaultService ?? "test-service",
            SubjectIdentity = new ApiKeyIdentity
            {
                ApiKey = settings.ApiKey ?? "test-api-key"
            },
            Operations = new[] { "read-project" },
            HierarchyLevel = 1
        };

        var response = await cerberusClient.CheckObjectsAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Objects);

        logger.LogInformation($"Получено объектов: {response.Objects.Length}");
        foreach (var obj in response.Objects)
        {
            logger.LogInformation($"Объект: {obj.Object}, Операции: {string.Join(", ", obj.Operations ?? Array.Empty<string>())}");
        }
    }

    [Fact(Skip = "Требуется подключение к рабочему Cerberus API")]
    public async Task TestCheckObjectsByName()
    {
        var settings = CerberusSettings.Default;
        var cerberusClient = new CerberusClient(settings.Url, settings.ApiKey, logger);

        var request = new CheckObjectsByNameRequest
        {
            Service = settings.DefaultService,
            SubjectIdentity = new AuthSidIdentity
            {
                SessionId = "...",
            },
            Objects = ["/forms"],
            Operations = ["read-project"],
            NeedPermissionsDetails = true
        };

        var response = await cerberusClient.CheckObjectsByNameAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Objects);

        logger.LogInformation($"Получено объектов: {response.Objects.Length}");
        foreach (var obj in response.Objects)
        {
            logger.LogInformation($"Объект: {obj.Object}, Операции: {string.Join(", ", obj.Operations ?? Array.Empty<string>())}");
        }

        if (response.PermissionsDetails != null)
        {
            logger.LogInformation("\nДетали разрешений:");
            foreach (var detail in response.PermissionsDetails)
            {
                logger.LogInformation($"  Объект: {detail.Object}");
                logger.LogInformation($"  Операции: {string.Join(", ", detail.Operations ?? Array.Empty<string>())}");
                if (detail.Sources != null)
                {
                    foreach (var source in detail.Sources)
                    {
                        logger.LogInformation($"    Источник: {source.Type} - {source.Name} (ID: {source.Id})");
                    }
                }
            }
        }
    }
}
