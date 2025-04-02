using System.Text;
using dotenv.net;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.Graphite;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.GitLabJobsCrawler;
using TestCity.Api.Extensions;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8125");

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddSingleton<GitLabSettings>(GitLabSettings.Default);

// builder.Services.AddSingleton<IVostokApplicationMetrics>(environment.Metrics)();
builder.Services.AddSingleton<JUnitExtractor>();
builder.Services.AddSingleton<TestMetricsSender>();
builder.Services.AddSingleton<GitLabCrawlerService>();
builder.Services.AddSingleton<SkbKonturGitLabClientProvider>();
builder.Services.AddSingleton<WorkerClient>();

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
        .ConfigureTestAnalyticsOpenTelemetry("TestAnalytics", "GitLabJobsCrawler", x => x.AddMeter("GitLabProjectTestsMetrics"));
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Services.GetRequiredService<GitLabCrawlerService>().Start();
app.Run();
