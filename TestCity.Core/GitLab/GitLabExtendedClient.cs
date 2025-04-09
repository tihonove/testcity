using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Kontur.TestCity.Core.GitLab.Models;

namespace Kontur.TestCity.Core.GitLab;

public sealed class GitLabExtendedClient : IDisposable
{
    public GitLabExtendedClient(string hostUrl, string privateToken)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{hostUrl.TrimEnd('/')}/api/v4/"),
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", privateToken);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
    }

    public async Task<PagedApiResponse<List<GitLabCommit>>> GetRepositoryCommitsAsync(
        long projectId,
        RepositoryCommitsQueryOptions? options = null)
    {
        options ??= new RepositoryCommitsQueryOptions();

        var queryParams = options.BuildQueryParameters();
        string url = $"projects/{projectId}/repository/commits";

        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await GetWithResponseAsync<List<GitLabCommit>>(url);
    }

    public async Task<PagedApiResponse<List<GitLabCommit>>> GetRepositoryCommitsAsync(
        long projectId,
        Action<RepositoryCommitsQueryOptions> optionsBuilder)
    {
        var options = new RepositoryCommitsQueryOptions();
        optionsBuilder(options);
        return await GetRepositoryCommitsAsync(projectId, options);
    }

    public async Task<GitLabJob> GetJobAsync(long projectId, long jobId)
    {
        var url = $"projects/{projectId}/jobs/{jobId}";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<GitLabJob>(content, jsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response to {nameof(GitLabJob)}");

        return result;
    }

    /// <summary>
    /// Retrieves all repository commits asynchronously using pagination
    /// </summary>
    /// <param name="projectId">ID of the GitLab project</param>
    /// <param name="options">Options for filtering and pagination of commits</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>An async enumerable of all commits matching the criteria</returns>
    public async IAsyncEnumerable<GitLabCommit> GetAllRepositoryCommitsAsync(
        long projectId,
        RepositoryCommitsQueryOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetRepositoryCommitsAsync(projectId, options);

        foreach (var commit in response.Result)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return commit;
        }

        var nextPageUrl = response.NextPageLink;
        while (!string.IsNullOrEmpty(nextPageUrl))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativeUrl = nextPageUrl.Replace(httpClient.BaseAddress!.ToString(), "");
            var nextPageResponse = await GetWithResponseAsync<List<GitLabCommit>>(relativeUrl);

            foreach (var commit in nextPageResponse.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return commit;
            }

            nextPageUrl = nextPageResponse.NextPageLink;
        }
    }

    /// <summary>
    /// Retrieves all repository commits asynchronously using pagination
    /// </summary>
    /// <param name="projectId">ID of the GitLab project</param>
    /// <param name="optionsBuilder">Action to configure commit query options</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>An async enumerable of all commits matching the criteria</returns>
    public async IAsyncEnumerable<GitLabCommit> GetAllRepositoryCommitsAsync(
        long projectId,
        Action<RepositoryCommitsQueryOptions> optionsBuilder,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var options = new RepositoryCommitsQueryOptions();
        optionsBuilder(options);
        
        await foreach (var commit in GetAllRepositoryCommitsAsync(projectId, options, cancellationToken))
        {
            yield return commit;
        }
    }

    public async Task<PagedApiResponse<List<GitLabJob>>> GetProjectJobsAsync(long projectId, JobScope? scope = null, int? page = null, int? perPage = null)
    {
        var url = $"projects/{projectId}/jobs";
        var queryParams = new List<string>();

        if (scope.HasValue && scope.Value != JobScope.None)
        {
            queryParams.AddRange(scope.Value.GetIndividualScopes()
                .Select(s => s.ToStringValue())
                .Select(s => $"scope[]={s}"));
        }

        if (page.HasValue)
        {
            queryParams.Add($"page={page.Value}");
        }

        if (perPage.HasValue)
        {
            queryParams.Add($"per_page={perPage.Value}");
        }

        if (queryParams.Count > 0)
        {
            url = $"{url}?{string.Join("&", queryParams)}";
        }

        return await GetWithResponseAsync<List<GitLabJob>>(url);
    }

    public async IAsyncEnumerable<GitLabJob> GetAllProjectJobsAsync(
        long projectId,
        JobScope? scope = null,
        int? perPage = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetProjectJobsAsync(projectId, scope, 1, perPage);

        foreach (var job in response.Result)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return job;
        }

        var nextPageUrl = response.NextPageLink;
        while (!string.IsNullOrEmpty(nextPageUrl))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativeUrl = nextPageUrl.Replace(httpClient.BaseAddress!.ToString(), "");
            var nextPageResponse = await GetWithResponseAsync<List<GitLabJob>>(relativeUrl);

            foreach (var job in nextPageResponse.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return job;
            }

            nextPageUrl = nextPageResponse.NextPageLink;
        }
    }

    private async Task<PagedApiResponse<T>> GetWithResponseAsync<T>(string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<T>(content, jsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}");

            TryGetHeaderValue(response.Headers, "X-Page", out var page);
            TryGetHeaderValue(response.Headers, "X-Per-Page", out var perPage);
            TryGetHeaderValue(response.Headers, "X-Total-Pages", out var totalPages);
            TryGetHeaderValue(response.Headers, "X-Total", out var totalItems);
            TryGetHeaderValue(response.Headers, "X-Next-Page", out var nextPage);
            TryGetHeaderValue(response.Headers, "X-Prev-Page", out var prevPage);

            return new PagedApiResponse<T>
            {
                Result = result,
                Headers = response.Headers,
                NextPageLink = ExtractNextPageLink(response.Headers),
                Page = page,
                PerPage = perPage,
                TotalPages = totalPages,
                TotalItems = totalItems,
                NextPage = nextPage,
                PrevPage = prevPage
            };
        }
        catch (Exception ex)
        {
            var requestInfo = $"Request URL: {url}, Method: GET";
            throw new HttpRequestException($"Request failed. {requestInfo}", ex);
        }
    }

    private static bool TryGetHeaderValue(HttpResponseHeaders headers, string headerName, out string? value)
    {
        if (headers.TryGetValues(headerName, out var values))
        {
            value = values.FirstOrDefault();
            return value != null;
        }

        value = null;
        return false;
    }

    private static string? ExtractNextPageLink(HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("Link", out var linkValues))
        {
            var linkValue = linkValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(linkValue))
            {
                // Parse the Link header to extract the "next" link
                // Format: <url>; rel="next"
                var matches = System.Text.RegularExpressions.Regex.Matches(linkValue, @"<([^>]+)>;\s*rel=""(\w+)""");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups[2].Value == "next")
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
        }

        return null;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions;
}
