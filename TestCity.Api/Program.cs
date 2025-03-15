using System.Text;
using dotenv.net;
using Kontur.TestAnalytics.Core;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8124");

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddSingleton<GitLabSettings>(new GitLabSettings() { GitLabToken = Environment.GetEnvironmentVariable("GITLAB_TOKEN") ?? "NoToken" });
builder.Services.AddSingleton<SkbKonturGitLabClientProvider>();

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "hh:mm:ss ";
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
// app.UseHttpsRedirection();
app.Run();
