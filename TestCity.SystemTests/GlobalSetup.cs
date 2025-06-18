using dotenv.net;
using TestCity.Core.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TestCity.UnitTests.Utils;
using System.Net.Http;
using TestCity.SystemTests.Api.ApiClient;

namespace TestCity.SystemTests;

public sealed class SystemTestsSetup : IAsyncLifetime
{
    private static Task servicesInitializationTask;
    private static readonly object initLock = new();
    private static bool isInitialized;
    private static ILogger logger;

    public SystemTestsSetup()
    {
        DotEnv.Fluent().WithProbeForEnv(10).Load();
        logger = Log.LoggerFactory.CreateLogger<SystemTestsSetup>();
    }

    private async Task WaitForServicesReadyAsync()
    {
        try
        {
            var apiUrl = Environment.GetEnvironmentVariable("TESTCITY_API_URL") ?? throw new InvalidOperationException("TESTCITY_API_URL environment variable is not set.");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiUrl)
            };

            var apiClient = new TestCityApiClient(httpClient);
            var startTime = DateTime.UtcNow;
            var timeBudget = TimeSpan.FromMinutes(5);

            logger.LogInformation("Ожидание доступности API сервиса...");

            while (DateTime.UtcNow - startTime < timeBudget)
            {
                try
                {
                    await apiClient.CheckHealth();
                    isInitialized = true;
                    logger.LogInformation("API сервис успешно запущен и доступен");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "API сервис пока не доступен. Повторная попытка через 1 секунду...");
                    await Task.Delay(1000);
                }
            }

            throw new TimeoutException($"API сервис не стал доступен в течение {timeBudget}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при инициализации сервисов");
            throw;
        }
        finally
        {
            isInitialized = true;
        }
    }

    public static ILoggerFactory TestLoggerFactory(ITestOutputHelper output)
    {
        XUnitLoggerProvider.ConfigureTestLogger(output);
        return Log.LoggerFactory;
    }

    public async Task InitializeAsync()
    {
        if (isInitialized)
            return;

        lock (initLock)
        {
            if (servicesInitializationTask == null)
            {
                logger.LogInformation("Начало проверки сервисов для системных тестов");
                servicesInitializationTask = WaitForServicesReadyAsync();
            }
        }

        await servicesInitializationTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

[CollectionDefinition("System")]
public class SystemCollection : ICollectionFixture<SystemTestsSetup>
{
}
