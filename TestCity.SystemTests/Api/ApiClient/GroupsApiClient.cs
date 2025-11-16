using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TestCity.Api.Models;
using TestCity.SystemTests.Api.ApiClient;

namespace TestCity.SystemTests.Api;

internal class GroupsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<List<GroupDto>?> GetRootGroups()
    {
        return await GetAsync<List<GroupDto>>("api/groups-v2");
    }

    public async Task<GroupNodeDto?> GetGroup(string idOrTitle)
    {
        return await GetAsync<GroupNodeDto>($"api/groups-v2/{idOrTitle}");
    }

    public async Task<HttpStatusCode> GetGroupStatusCode(string idOrTitle)
    {
        var response = await GetResponseAsync($"api/groups-v2/{idOrTitle}");
        return response.StatusCode;
    }
}
