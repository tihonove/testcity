using System.Text;
using dotenv.net;
using Kontur.TestCity.Worker.Handlers;
using Kontur.TestCity.Worker.Handlers.Base;
using Kontur.TestCity.Worker.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestCity.Api.Extensions;
using TestCity.Worker.Kafka;
using TestCity.Worker.Kafka.Configuration;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(KafkaConsumerSettings.Default);
        services.AddSingleton(BatchProcessorSettings.Default);
        services.AddSingleton<ITaskHandler, ProcessJobRunTaskHandler>();

        services.AddSingleton<ITaskHandlerRegistry, TaskHandlerRegistry>();
        services.AddSingleton<IHostedService, KafkaConsumerService>();

        if (OpenTelemetryExtensions.IsOpenTelemetryEnabled())
        {
            services
                .AddOpenTelemetry()
                .ConfigureTestAnalyticsOpenTelemetry("TestAnalytics", "Worker");
        }
    })
    .ConfigureLogging((hostContext, logging) =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss ";
        });
    })
    .Build();

await host.RunAsync();
