using System.Text;
using Kontur.TestAnalytics.Api;
using Vostok.Hosting;
using Vostok.Hosting.Houston;
using Vostok.Hosting.Houston.Abstractions;
using Vostok.Hosting.Setup;

[assembly: HoustonEntryPoint(typeof(TestAnalyticsApiApplication))]

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = new HoustonHost(
    config =>
    {
        config.Everywhere.SetupEnvironment(
            builder =>
            {
                builder.SetPort(8124);
                builder.SetBaseUrlPath("test-analytics");
                builder.SetBeaconApplication("test-analytics");
                builder.SetupApplicationIdentity(
                    idBuilder => idBuilder
                        .SetProject("TestAnalytics")
                        .SetApplication("Api")
                );
            }
        );
    }
);

await host.WithConsoleCancellation().RunAsync();