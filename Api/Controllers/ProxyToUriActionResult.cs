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
        var request = new HttpRequestMessage(new HttpMethod(sourceRequest.Method), targetUri + sourceRequest.QueryString);
        foreach (var sourceRequestHeader in sourceRequest.Headers)
        {
            if (sourceRequestHeader.Key.StartsWith("X-") || sourceRequestHeader.Key == "Authorization")
            {
                request.Headers.Add(sourceRequestHeader.Key, sourceRequestHeader.Value.AsEnumerable<string>());
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
