using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestCity.Core.Clickhouse;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/clickhouse")]
[Authorize]
public class ClickhouseProxyController(ClickHouseConnectionSettings clickHouseConnectionSettings) : Controller
{
    [Route("{*query}")]
    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    public IActionResult Proxy(string? query)
    {
        var host = clickHouseConnectionSettings.Host;
        var port = clickHouseConnectionSettings.Port;
        return new ProxyToUriActionResult(
            clickHouseConnectionSettings,
            Request,
            $"http://{host}:{port}/{query}");
    }
}
