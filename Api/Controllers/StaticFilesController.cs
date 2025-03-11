using Kontur.TestAnalytics.Front;
using Microsoft.AspNetCore.Mvc;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Api.Controllers;

[ApiController]
[Route("")]
public class StaticFilesController : ControllerBase
{
    [Route("")]
    [Route("{*pathInfo:regex(^(?!static|clickhouse|gitlab).*$)}")]
    [HttpGet]
    public ContentResult Home(string? pathInfo)
    {
        var apiPrefix = Environment.GetEnvironmentVariable("TESTANALYTICS_API_PREFIX") ?? throw new Exception("TESTANALYTICS_API_PREFIX is not set");
        this.indexHtmlContent ??= TestAnalyticsFrontContent.GetFileContent("index.html")
            .Replace("__webpack_public_path__ = \"/", $"__webpack_public_path__ = \"/{apiPrefix}/")
            .Replace("src=\"/", $"src=\"/{apiPrefix}/")
            .Replace("href=\"/", $"href=\"/{apiPrefix}/");
        return Content(this.indexHtmlContent, "text/html");
    }

    [Route("static/{*pathInfo}")]
    [HttpGet]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Allow in controllers")]
    public Stream StaticFiles(string pathInfo) => TestAnalyticsFrontContent.GetFile("static/" + pathInfo);

    private readonly ILog log = LogProvider.Get().ForContext<StaticFilesController>();
    private string indexHtmlContent;
}
