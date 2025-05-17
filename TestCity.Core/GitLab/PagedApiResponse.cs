using System.Net.Http.Headers;

namespace TestCity.Core.GitLab;

public class PagedApiResponse<T>
{
    public required T Result { get; init; }

    public required HttpResponseHeaders Headers { get; init; }

    public string? NextPageLink { get; init; }

    // Additional pagination information from headers
    public string? Page { get; init; }

    public string? PerPage { get; init; }

    public string? TotalPages { get; init; }

    public string? TotalItems { get; init; }

    public string? NextPage { get; init; }

    public string? PrevPage { get; init; }
}
