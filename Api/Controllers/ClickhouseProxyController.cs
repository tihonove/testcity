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
        var Host = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_HOST is not set");
        var Port = ushort.Parse(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PORT is not set"));
        return new ProxyToUriActionResult(
            Request,
            $"http://{Host}:{Port}/{query}");
    }
}
