using System.Text;
using dotenv.net;
using Kontur.TestAnalytics.Api;
using Vostok.Configuration.Sources.Environment;
using Vostok.Hosting;
using Vostok.Hosting.Houston;
using Vostok.Hosting.Houston.Abstractions;
using Vostok.Hosting.Setup;

[assembly: HoustonEntryPoint(typeof(TestAnalyticsApiApplication))]

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = new HoustonHost(
    config =>
    {
        config.OutOfHouston.SetupEnvironment(builder =>
        {
            builder.SetPort(8124);
            builder.SetupConfiguration(config => config.AddSecretSource(new EnvironmentVariablesSource()));
        });

        config.Everywhere.SetupEnvironment(
            builder =>
            {
                var apiPrefix = Environment.GetEnvironmentVariable("TESTANALYTICS_API_PREFIX") ?? throw new Exception("TESTANALYTICS_API_PREFIX is not set");
                builder.SetBaseUrlPath(apiPrefix);
                builder.SetBeaconApplication(apiPrefix);
                builder.SetupApplicationIdentity(
                    idBuilder => idBuilder
                        .SetProject("TestAnalytics")
                        .SetApplication("Api"));
            });
    });

await host.WithConsoleCancellation().RunAsync();
