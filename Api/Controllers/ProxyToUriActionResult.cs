using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Kontur.TestAnalytics.Api.Controllers;

internal sealed class ProxyToUriActionResult : IActionResult
{
    public ProxyToUriActionResult(HttpRequest sourceRequest, string targetUri, IReadOnlyDictionary<string, string>? additionalHeaders = null)
    {
        this.sourceRequest = sourceRequest;
        this.targetUri = targetUri;
        this.additionalHeaders = additionalHeaders ?? new Dictionary<string, string>();
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };

        using var httpClient = new HttpClient(handler);
        var queryString = sourceRequest.QueryString.ToString();
        if (!string.IsNullOrEmpty(queryString))
        {
            var database = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_DB is not set");
            queryString = queryString.Replace("database=test_analytics", $"database={database}");
        }
        var request = new HttpRequestMessage(new HttpMethod(sourceRequest.Method), targetUri + queryString);
        foreach (var sourceRequestHeader in sourceRequest.Headers)
        {
            if (sourceRequestHeader.Key.StartsWith("X-"))
            {
                request.Headers.Add(sourceRequestHeader.Key, sourceRequestHeader.Value.AsEnumerable<string>());
            }
            if (sourceRequestHeader.Key == "Authorization")
            {
                var user = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER");
                var password = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD");
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
                request.Headers.Add("Authorization", $"Basic {base64authorization}");
            }
        }

        foreach (var additionalHeader in additionalHeaders)
        {
            request.Headers.Add(additionalHeader.Key, additionalHeader.Value);
        }

        if (sourceRequest.Method != "GET")
        {
            request.Content = new StreamContent(sourceRequest.Body);
        }

        var responseFromTarget = await httpClient.SendAsync(request);
        var clientResponse = context.HttpContext.Response;

        foreach (var responseHeader in responseFromTarget.Headers)
        {
            if (responseHeader.Key.StartsWith("X-"))
            {
                clientResponse.Headers.TryAdd(responseHeader.Key, new StringValues(responseHeader.Value.ToArray()));
            }
        }

        clientResponse.StatusCode = (int)responseFromTarget.StatusCode;

        await using var responseStream = await responseFromTarget.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(clientResponse.Body);
    }

    private readonly HttpRequest sourceRequest;
    private readonly string targetUri;
    private readonly IReadOnlyDictionary<string, string> additionalHeaders;
}
