using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TestCity.SystemTests.Api.ApiClient;

internal abstract class ApiClientBase(HttpClient httpClient)
{
    protected HttpClient HttpClient { get; } = httpClient;

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<HttpResponseMessage> GetResponseAsync(string url)
    {
        return await HttpClient.GetAsync(url);
    }

    protected async Task<T?> PostAsync<T>(string url, object? content = null)
    {
        var response = await HttpClient.PostAsync(url, CreateJsonContent(content));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<HttpResponseMessage> PostResponseAsync(string url, object? content = null)
    {
        return await HttpClient.PostAsync(url, CreateJsonContent(content));
    }

    private static HttpContent CreateJsonContent(object? content)
    {
        if (content == null)
            return new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var json = JsonSerializer.Serialize(content);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
