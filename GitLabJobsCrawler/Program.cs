using System.Text;
using dotenv.net;
using Kontur.TestAnalytics.Api;
using Vostok.Configuration.Sources.Environment;
using Vostok.Hosting;
using Vostok.Hosting.Houston;
using Vostok.Hosting.Houston.Abstractions;
using Vostok.Hosting.Setup;

[assembly: HoustonEntryPoint(typeof(TestAnalyticsGitLabCrawlerApplication))]

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = new HoustonHost(
    config =>
    {
        config.OutOfHouston.SetupEnvironment(builder =>
        {
            builder.SetPort(8125);
            builder.SetupConfiguration(config => config.AddSecretSource(new EnvironmentVariablesSource()));
        });

        config.Everywhere.SetupEnvironment(
            builder =>
            {
                var apiPrefix = Environment.GetEnvironmentVariable("TESTANALYTICS_API_PREFIX") ?? throw new Exception("TESTANALYTICS_API_PREFIX is not set");
                builder.SetBaseUrlPath(apiPrefix + "-gitlab-crawler");
                builder.SetBeaconApplication(apiPrefix + "-gitlab-crawler");
                builder.SetupApplicationIdentity(
                    idBuilder => idBuilder
                        .SetProject("TestAnalytics")
                        .SetApplication("GitLabCrawler"));
            });
    });

await host.WithConsoleCancellation().RunAsync();
