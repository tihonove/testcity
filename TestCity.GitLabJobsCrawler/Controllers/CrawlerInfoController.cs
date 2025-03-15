using System.Text.Json.Nodes;
using Kontur.TestCity.GitLabJobsCrawler;
using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;

[ApiController]
public class CrawlerInfoController : Controller
{
    public CrawlerInfoController(GitLabCrawlerService crawlerService, ILogger<CrawlerInfoController> log)
    {
        this.crawlerService = crawlerService;
        this.log = log;
    }

    [Route("info")]
    [AcceptVerbs("GET")]
    public IActionResult Info(string? query)
    {
        return Content("Hello!");
    }

    [Route("gitlab")]
    [AcceptVerbs("POST")]
    public IActionResult AcceptWebHook([FromBody] JsonObject content)
    {
        log.LogInformation("GitLafb webhook");
        log.LogInformation("Content: {content}", content.ToString());
        return Ok();
    }

    private readonly GitLabCrawlerService crawlerService;
    private readonly ILogger<CrawlerInfoController> log;
}
