namespace TestCity.UnitTests.Api.ApiClient;

internal class ResetCacheApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<ResetCacheResponse?> ResetAllCaches()
    {
        return await PostAsync<ResetCacheResponse>("api/reset");
    }

    internal class ResetCacheResponse
    {
        public string? Message { get; set; }
    }
}
