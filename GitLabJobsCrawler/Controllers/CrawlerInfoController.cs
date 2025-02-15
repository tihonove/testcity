using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Vostok.Logging.Abstractions;

namespace Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;

[ApiController]
public class CrawlerInfoController : ControllerBase
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

    [Route("gitlab")]
    [AcceptVerbs("POST")]
    public IActionResult AcceptWebHook([FromBody] JsonObject content)
    {
        log.Info("GitLab webhook");
        log.Info(content.ToString());
        return Ok();
    }

    private readonly ILog log = LogProvider.Get();

    private readonly GitLabCrawlerService crawlerService;
}
