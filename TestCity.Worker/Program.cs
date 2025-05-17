using System.Text;
using dotenv.net;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Graphite;
using TestCity.Core.Infrastructure;
using TestCity.Core.JobProcessing;
using TestCity.Core.JUnit;
using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using TestCity.Core.Worker;
using TestCity.Worker.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;

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
        services.AddSingleton<GitLabProjectsService>();
        services.AddSingleton<ConnectionFactory>();
        services.AddSingleton<TestCityDatabase>();
        services.AddSingleton<ProjectJobTypesCache>();
        services.AddSingleton<CommitParentsBuilderService>();
        services.AddSingleton(r => KafkaMessageQueueClient.CreateDefault(r.GetRequiredService<ILogger<KafkaMessageQueueClient>>()));
        services.AddSingleton<ITaskHandler, ProcessJobRunTaskHandler>();
        services.AddSingleton<ITaskHandler, BuildCommitParentsHandler>();
        services.AddSingleton<ITaskHandler, ProcessInProgressJobTaskHandler>();

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
