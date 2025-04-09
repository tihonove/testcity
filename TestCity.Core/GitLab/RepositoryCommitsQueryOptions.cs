namespace Kontur.TestCity.Core;

/// <summary>
/// Builder class for repository commits query options
/// </summary>
public class RepositoryCommitsQueryOptions
{
    // Commit filtering options
    public string? RefName { get; set; }
    public DateTime? Since { get; set; }
    public DateTime? Until { get; set; }
    public string? Path { get; set; }
    public string? Author { get; set; }
    public bool? All { get; set; }
    public bool? WithStats { get; set; }
    public bool? FirstParent { get; set; }
    public string? Order { get; set; }
    public bool? Trailers { get; set; }

    // Pagination options
    public string? Pagination { get; set; }
    public int? PerPage { get; set; }
    public string? OrderBy { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
    public string? IdAfter { get; set; }

    /// <summary>
    /// Configures options for keyset pagination
    /// </summary>
    /// <param name="perPage">Number of items per page</param>
    /// <param name="orderBy">Field to order by</param>
    /// <param name="sort">Sort direction (asc or desc)</param>
    /// <returns>This query options instance for chaining</returns>
    public RepositoryCommitsQueryOptions UseKeysetPagination(int? perPage = null, string? orderBy = null, string? sort = null)
    {
        Pagination = "keyset";
        PerPage = perPage;
        OrderBy = orderBy;
        Sort = sort;
        return this;
    }

    /// <summary>
    /// Configures date range filtering
    /// </summary>
    /// <param name="since">Include commits after this date</param>
    /// <param name="until">Include commits before this date</param>
    /// <returns>This query options instance for chaining</returns>
    public RepositoryCommitsQueryOptions WithDateRange(DateTime? since = null, DateTime? until = null)
    {
        Since = since;
        Until = until;
        return this;
    }

    /// <summary>
    /// Configures filtering by specific reference (branch, tag, etc.)
    /// </summary>
    /// <param name="refName">The name of a repository branch, tag or revision range</param>
    /// <returns>This query options instance for chaining</returns>
    public RepositoryCommitsQueryOptions ForReference(string refName)
    {
        RefName = refName;
        return this;
    }

    /// <summary>
    /// Builds a list of query parameters for the API request
    /// </summary>
    /// <returns>List of query parameters</returns>
    internal List<string> BuildQueryParameters()
    {
        var queryParams = new List<string>();

        // Add commit filtering parameters
        AddParameter(queryParams, "ref_name", RefName);
        AddDateParameter(queryParams, "since", Since);
        AddDateParameter(queryParams, "until", Until);
        AddParameter(queryParams, "path", Path);
        AddParameter(queryParams, "author", Author);
        AddBoolParameter(queryParams, "all", All);
        AddBoolParameter(queryParams, "with_stats", WithStats);
        AddBoolParameter(queryParams, "first_parent", FirstParent);
        AddParameter(queryParams, "order", Order);
        AddBoolParameter(queryParams, "trailers", Trailers);

        // Add pagination parameters
        AddParameter(queryParams, "pagination", Pagination);
        AddIntParameter(queryParams, "per_page", PerPage);
        AddParameter(queryParams, "order_by", OrderBy);
        AddParameter(queryParams, "sort", Sort);
        AddParameter(queryParams, "cursor", Cursor);
        AddParameter(queryParams, "id_after", IdAfter);

        return queryParams;
    }

    private void AddParameter(List<string> queryParams, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            queryParams.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    private void AddBoolParameter(List<string> queryParams, string name, bool? value)
    {
        if (value.HasValue)
        {
            queryParams.Add($"{name}={value.Value.ToString().ToLowerInvariant()}");
        }
    }

    private void AddIntParameter(List<string> queryParams, string name, int? value)
    {
        if (value.HasValue)
        {
            queryParams.Add($"{name}={value.Value}");
        }
    }

    private void AddDateParameter(List<string> queryParams, string name, DateTime? value)
    {
        if (value.HasValue)
        {
            queryParams.Add($"{name}={value.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
    }
}
