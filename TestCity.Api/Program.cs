using System.Text;
using dotenv.net;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.Infrastructure;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Worker;
using OpenTelemetry.Metrics;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8124");

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddSingleton<GitLabSettings>(new GitLabSettings() { Token = Environment.GetEnvironmentVariable("GITLAB_TOKEN") ?? "NoToken" });
builder.Services.AddSingleton<SkbKonturGitLabClientProvider>();
builder.Services.AddSingleton<WorkerClient>();
builder.Services.AddSingleton<ConnectionFactory>();
builder.Services.AddSingleton<TestCityDatabase>();
builder.Services.AddSingleton(r => KafkaMessageQueueClient.CreateDefault(r.GetRequiredService<ILogger<KafkaMessageQueueClient>>()));
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
        .ConfigureTestAnalyticsOpenTelemetry("TestAnalytics", "Api", metrics =>
        {
            metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddMeter("System.Net.Http")
                .AddMeter("GitLabProjectTestsMetrics");
        });
}

var app = builder.Build();
Log.ConfigureGlobalLogProvider(app.Services.GetRequiredService<ILoggerFactory>());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
