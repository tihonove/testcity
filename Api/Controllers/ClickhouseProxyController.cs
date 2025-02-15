using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestAnalytics.Api.Controllers;

[ApiController]
[Route("clickhouse")]
public class ClickhouseProxyController : ControllerBase
{
    [Route("{*query}")]
    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    public IActionResult Proxy(string? query)
    {
        return new ProxyToUriActionResult(
            Request,
            $"http://vm-ch2-stg.dev.kontur.ru:8123/{query}");
    }
}
