using Kontur.TestAnalytics.Api;
using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;

[ApiController]
internal class CrawlerInfoController : ControllerBase
{
    public CrawlerInfoController(GitLabCrawlerService crawlerService)
    {
        this.crawlerService = crawlerService;
    }

    [Route("info")]
    [AcceptVerbs("GET")]
    public IActionResult Info(string? query)
    {
        return Content("Hello!");
    }

    private readonly GitLabCrawlerService crawlerService;
}