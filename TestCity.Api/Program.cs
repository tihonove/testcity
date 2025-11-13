using System.Text;
using dotenv.net;
using Microsoft.AspNetCore.DataProtection;
using TestCity.Api.Authorization;
using TestCity.Api.Exceptions;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Infrastructure;
using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using TestCity.Core.Worker;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8124");

builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<HttpStatusExceptionFilter>();
});

var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "TestCity_";
});

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString), "DataProtection-Keys");

builder.Services.AddSingleton(GitLabSettings.Default);
builder.Services.AddSingleton(ClickHouseConnectionSettings.Default);
builder.Services.AddSingleton(AuthorizationSettings.Default);
builder.Services.AddSingleton<SkbKonturGitLabClientProvider>();
builder.Services.AddSingleton<WorkerClient>();
builder.Services.AddSingleton<GitLabProjectsService>();
builder.Services.AddSingleton<IResetable, GitLabProjectsService>();
builder.Services.AddSingleton<ConnectionFactory>();
builder.Services.AddSingleton<TestCityDatabase>();
builder.Services.AddSingleton<ProjectJobTypesCache>();
builder.Services.AddSingleton<GitLabPathResolver>();
builder.Services.AddSingleton(r => KafkaMessageQueueClient.CreateDefault(r.GetRequiredService<ILogger<KafkaMessageQueueClient>>()));

var authSettings = AuthorizationSettings.Default;

if (authSettings.Type == AuthorizationType.OpenIdConnect)
{
    if (authSettings.Oidc == null)
    {
        throw new InvalidOperationException("OIDC settings are not configured");
    }
    builder.Services
        .AddAuthentication(o => { o.DefaultScheme = "Cookies"; o.DefaultChallengeScheme = "oidc"; })
        .AddCookie("Cookies")
        .AddOpenIdConnect("oidc", o =>
        {
            o.Authority = authSettings.Oidc.Authority;
            o.RequireHttpsMetadata = true;
            o.ClientId = authSettings.Oidc.ClientId;
            o.ClientSecret = authSettings.Oidc.ClientSecret;
            o.ResponseType = "code";
            o.UsePkce = true;
            o.SaveTokens = true;
            o.Scope.Clear();
            o.Scope.Add("openid");
            o.Scope.Add("profile");
            o.CallbackPath = "/signin-oidc";
        });
}
else
{
    builder.Services.AddAuthentication("NoAuth").AddScheme<NoAuthSchemeOptions, NoAuthHandler>("NoAuth", _ => { });
    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ConditionalAuthorizationHandler>();
}

builder.Services.AddAuthorization();
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
                .AddMeter("GitLabProjectTestsMetrics")
                .AddMeter("UserActivityMetrics");
        });
}

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    RequireHeaderSymmetry = false,
    ForwardLimit = null,
    KnownNetworks = { },
    KnownProxies = { }
});
if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";
        return next();
    });
}
app.UseAuthentication();
app.UseAuthorization();
Log.ConfigureGlobalLogProvider(app.Services.GetRequiredService<ILoggerFactory>());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
