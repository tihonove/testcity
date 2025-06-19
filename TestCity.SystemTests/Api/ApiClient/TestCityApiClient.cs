namespace TestCity.SystemTests.Api.ApiClient;

internal class TestCityApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<ResetCacheResponse?> ResetAllCaches()
    {
        return await PostAsync<ResetCacheResponse>("api/reset");
    }

    public async Task CheckHealth()
    {
        var response = await GetResponseAsync("api/health");
        response.EnsureSuccessStatusCode();
    }

    internal class ResetCacheResponse
    {
        public string? Message { get; set; }
    }
}
