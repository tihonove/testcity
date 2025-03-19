using System.Text;
using dotenv.net;
using Kontur.TestAnalytics.Core;
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
        .ConfigureTestAnalyticsOpenTelemetry("TestCity", "GitLabJobsCrawler", x => x.AddMeter("GitLabProjectTestsMetrics"));
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Services.GetRequiredService<GitLabCrawlerService>().Start();
app.Run();
