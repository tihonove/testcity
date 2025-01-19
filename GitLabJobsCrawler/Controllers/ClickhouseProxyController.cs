using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;

[ApiController]
public class CrawlerInfoController : ControllerBase
{
    [Route("info")]
    [AcceptVerbs("GET")]
    public IActionResult Info(string? query)
    {
        return Content("Hello!");
    }
}