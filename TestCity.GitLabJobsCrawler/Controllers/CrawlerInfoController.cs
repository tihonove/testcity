using Kontur.TestCity.Core.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;

[ApiController]
public class CrawlerInfoController : Controller
{
    [Route("info")]
    [AcceptVerbs("GET")]
    public IActionResult Info(string? query)
    {
        return Content("Hello!");
    }

    private readonly ILogger log = Log.GetLog<CrawlerInfoController>();
}
