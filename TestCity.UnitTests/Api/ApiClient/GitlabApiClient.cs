using System.Net;
using System.Text;

namespace TestCity.UnitTests.Api.ApiClient;

internal class GitlabApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<string?> GetCodeQuality(long projectId, long jobId)
    {
        return await GetAsync<string>($"api/gitlab/{projectId}/jobs/{jobId}/codequality");
    }

    public async Task<HttpStatusCode> GetCodeQualityStatusCode(long projectId, long jobId)
    {
        var response = await GetResponseAsync($"api/gitlab/{projectId}/jobs/{jobId}/codequality");
        return response.StatusCode;
    }

    public async Task<HttpResponseMessage> PostWebhook(object jobEventInfo)
    {
        return await PostResponseAsync("api/gitlab/webhook", jobEventInfo);
    }

    public async Task<string?> PostWebhookWithRawData(string rawData)
    {
        using var content = new StringContent(rawData, Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync("api/gitlab/webhook", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string?> CheckProjectAccess(long projectId)
    {
        return await GetAsync<string>($"api/gitlab/projects/{projectId}/access-check");
    }

    public async Task<HttpStatusCode> CheckProjectAccessStatusCode(long projectId)
    {
        var response = await GetResponseAsync($"api/gitlab/projects/{projectId}/access-check");
        return response.StatusCode;
    }

    public async Task<string?> AddProject(long projectId)
    {
        return await PostAsync<string>($"api/gitlab/projects/{projectId}/add");
    }

    public async Task<HttpStatusCode> AddProjectStatusCode(long projectId)
    {
        var response = await PostResponseAsync($"api/gitlab/projects/{projectId}/add");
        return response.StatusCode;
    }

    public async Task<ManualJobRunInfo[]?> GetManualJobInfos(long projectId, long pipelineId)
    {
        return await GetAsync<ManualJobRunInfo[]>($"api/gitlab/{projectId}/pipelines/{pipelineId}/manual-jobs");
    }
}
