using System.Text;
using dotenv.net;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.Logging;

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
Log.ConfigureGlobalLogProvider(app.Services.GetRequiredService<ILoggerFactory>());

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
app.Use(async (HttpContext context, Func<Task> next) =>
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
});
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

app.Run();
