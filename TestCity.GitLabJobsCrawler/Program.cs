using System.Text;
using dotenv.net;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitlabProjects;
using Kontur.TestCity.Core.Graphite;
using Kontur.TestCity.Core.Infrastructure;
using Kontur.TestCity.Core.JobProcessing;
using Kontur.TestCity.Core.JUnit;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.GitLabJobsCrawler;
using OpenTelemetry.Metrics;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8125");

builder.Services.AddControllers();

builder.Services.AddSingleton(GitLabSettings.Default);

// builder.Services.AddSingleton<IVostokApplicationMetrics>(environment.Metrics)();
builder.Services.AddSingleton<JUnitExtractor>();
builder.Services.AddSingleton<TestMetricsSender>();
builder.Services.AddSingleton<GitLabCrawlerService>();
builder.Services.AddSingleton<SkbKonturGitLabClientProvider>();
builder.Services.AddSingleton<WorkerClient>();
builder.Services.AddSingleton<GitLabProjectsService>();
builder.Services.AddSingleton<ConnectionFactory>();
builder.Services.AddSingleton<TestCityDatabase>();
builder.Services.AddSingleton<ProjectJobTypesCache>();
builder.Services.AddSingleton(r => KafkaMessageQueueClient.CreateDefault(r.GetRequiredService<ILogger<KafkaMessageQueueClient>>()));

// Регистрация IGraphiteClient на основе переменных окружения
var graphiteHost = Environment.GetEnvironmentVariable("GRAPHITE_RELAY_HOST");
var graphitePortStr = Environment.GetEnvironmentVariable("GRAPHITE_RELAY_PORT");

if (!string.IsNullOrEmpty(graphiteHost) && !string.IsNullOrEmpty(graphitePortStr) && int.TryParse(graphitePortStr, out var graphitePort))
{
    builder.Services.AddSingleton<IGraphiteClient>(_ => new GraphiteClient(graphiteHost, graphitePort));
}
else
{
    builder.Services.AddSingleton<IGraphiteClient, NullGraphiteClient>();
}

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "hh:mm:ss ";
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

if (OpenTelemetryExtensions.IsOpenTelemetryEnabled())
{
    builder.Services
        .AddOpenTelemetry()
        .ConfigureTestAnalyticsOpenTelemetry("TestAnalytics", "GitLabJobsCrawler", metrics =>
        {
            metrics
                .AddRuntimeInstrumentation()
                .AddMeter("System.Net.Http")
                .AddMeter("GitLabProjectTestsMetrics");
        });
}

var app = builder.Build();
Log.ConfigureGlobalLogProvider(app.Services.GetRequiredService<ILoggerFactory>());
app.MapControllers();
app.Services.GetRequiredService<GitLabCrawlerService>().Start();
app.Run();
