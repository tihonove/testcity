using Kontur.TestAnalytics.Front;
using Microsoft.AspNetCore.Mvc;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.Api.Controllers;

[ApiController]
[Route("")]
public class StaticFilesController : ControllerBase
{
    public StaticFilesController(ILog log)
    {
        this.log = log;
    }

    [Route("")]
    [Route("history")]
    [Route("jobs/{*pathInfo}")]
    [Route("projects/{*pathInfo}")]
    [HttpGet]
    public Stream Home() => TestAnalyticsFrontContent.GetFile("index.html");

    [Route("static/{*pathInfo}")]
    [HttpGet]
    public Stream StaticFiles(string pathInfo) => TestAnalyticsFrontContent.GetFile("static/" + pathInfo);
    private readonly ILog log;
}
