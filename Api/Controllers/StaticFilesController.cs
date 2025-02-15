using Kontur.TestAnalytics.Front;
using Microsoft.AspNetCore.Mvc;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Api.Controllers;

[ApiController]
[Route("")]
public class StaticFilesController : ControllerBase
{
    public StaticFilesController()
    {
        log = LogProvider.Get().ForContext<StaticFilesController>();
    }

    [Route("")]
    [Route("{*pathInfo:regex(^(?!static|clickhouse|gitlab).*$)}")]
    [HttpGet]
    public Stream Home(string? pathInfo)
    {
        log.Info($"Access to {pathInfo}");
        return TestAnalyticsFrontContent.GetFile("index.html");
    }

    [Route("static/{*pathInfo}")]
    [HttpGet]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Allow in controllers")]
    public Stream StaticFiles(string pathInfo) => TestAnalyticsFrontContent.GetFile("static/" + pathInfo);

    private readonly ILog log;
}
