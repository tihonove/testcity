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
    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    public Stream Home() => TestAnalyticsFrontContent.GetFile("index.html");

    [Route("history")]
    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    public Stream HistoryHome() => TestAnalyticsFrontContent.GetFile("history/index.html");

    [Route("static/{*pathInfo}")]
    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    public Stream StaticFiles(string pathInfo) => TestAnalyticsFrontContent.GetFile("static/" + pathInfo);

    private readonly ILog log;
}
