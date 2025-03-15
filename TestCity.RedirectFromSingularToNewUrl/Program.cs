using System.Text;
using dotenv.net;
using Kontur.TestAnalytics.Core;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8122");
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "hh:mm:ss ";
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var request = context.Request;
    if (request.Path.StartsWithSegments("/test-analytics", out var remainingPath))
    {
        var newUrl = $"https://testcity.kube.testkontur.ru{remainingPath}{request.QueryString}";
        context.Response.Redirect(newUrl, permanent: true);
        return;
    }
    else
    {
        var newUrl = $"https://testcity.kube.testkontur.ru{request.Path}{request.QueryString}";
        context.Response.Redirect(newUrl, permanent: true);
        return;
    }

    await next();
});

app.Run();
