using System.Text;
using Kontur.TestAnalytics.Api;
using Vostok.Hosting;
using Vostok.Hosting.Houston;
using Vostok.Hosting.Houston.Abstractions;
using Vostok.Hosting.Setup;

[assembly: HoustonEntryPoint(typeof(TestAnalyticsGitLabCrawlerApplication))]

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = new HoustonHost(
    config =>
    {
        config.OutOfHouston.SetupEnvironment(
            builder => builder.SetPort(8125));

        config.Everywhere.SetupEnvironment(
            builder =>
            {
                builder.SetBaseUrlPath("test-analytics-gitlab-crawler");
                builder.SetBeaconApplication("test-analytics-gitlab-crawler");
                builder.SetupApplicationIdentity(
                    idBuilder => idBuilder
                        .SetProject("TestAnalytics")
                        .SetApplication("GitLabCrawler"));
            });
    });

await host.WithConsoleCancellation().RunAsync();
