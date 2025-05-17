using Microsoft.AspNetCore.Mvc;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/clickhouse")]
public class ClickhouseProxyController : Controller
{
    [Route("{*query}")]
    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    public IActionResult Proxy(string? query)
    {
        var host = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_HOST is not set");
        var port = ushort.Parse(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PORT is not set"));
        return new ProxyToUriActionResult(
            Request,
            $"http://{host}:{port}/{query}");
    }
}
