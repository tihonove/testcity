using System.Text;
using dotenv.net;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.Graphite;
using Kontur.TestCity.Core.Infrastructure;
using Kontur.TestCity.Core.JUnit;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.GitLabJobsCrawler;
using Kontur.TestCity.Worker.Handlers;
using Kontur.TestCity.Worker.Handlers.Base;
using Kontur.TestCity.Worker.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using TestCity.Worker.Kafka.Configuration;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(GitLabSettings.Default);

        services.AddSingleton<JUnitExtractor>();
        services.AddSingleton<TestMetricsSender>();
        services.AddSingleton<SkbKonturGitLabClientProvider>();
        services.AddSingleton<WorkerClient>();
        services.AddSingleton<ConnectionFactory>();
        services.AddSingleton<TestCityDatabase>();
        services.AddSingleton(r => KafkaMessageQueueClient.CreateDefault(r.GetRequiredService<ILogger<KafkaMessageQueueClient>>()));
        services.AddSingleton<ITaskHandler, ProcessJobRunTaskHandler>();
        services.AddSingleton<ITaskHandler, BuildCommitParentsHandler>();

        var graphiteHost = Environment.GetEnvironmentVariable("GRAPHITE_RELAY_HOST");
        var graphitePortStr = Environment.GetEnvironmentVariable("GRAPHITE_RELAY_PORT");

        if (!string.IsNullOrEmpty(graphiteHost) && !string.IsNullOrEmpty(graphitePortStr) && int.TryParse(graphitePortStr, out var graphitePort))
        {
            services.AddSingleton<IGraphiteClient>(_ => new GraphiteClient(graphiteHost, graphitePort));
        }
        else
        {
            services.AddSingleton<IGraphiteClient, NullGraphiteClient>();
        }

        services.AddSingleton<TaskHandlerRegistry>();
        services.AddSingleton<IHostedService>(r =>
        {
            return new KafkaMessageQueueConsumerBuilder()
                .WithSettings(KafkaConsumerSettings.Default)
                .WithTaskHandlers(r.GetServices<ITaskHandler>())
                .WithLoggerFactory(r.GetRequiredService<ILoggerFactory>())
                .BuildBackgroundService();
        });

        if (OpenTelemetryExtensions.IsOpenTelemetryEnabled())
        {
            services
                .AddOpenTelemetry()
                .ConfigureTestAnalyticsOpenTelemetry("TestAnalytics", "Worker", metrics =>
                {
                    metrics
                        .AddRuntimeInstrumentation()
                        .AddMeter(KafkaMessageQueueConsumer.QueueMeterName)
                        .AddMeter("System.Net.Http");
                });
        }
    })
    .ConfigureLogging((hostContext, logging) =>
    {
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss ";
        });
    })
    .Build();
Log.ConfigureGlobalLogProvider(host.Services.GetRequiredService<ILoggerFactory>());
await host.RunAsync();
