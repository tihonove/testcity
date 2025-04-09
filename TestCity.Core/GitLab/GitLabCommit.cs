namespace Kontur.TestCity.Core.GitLab;

using System.Text.Json.Serialization;

public class GitLabCommit
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("short_id")]
    public string ShortId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [JsonPropertyName("author_email")]
    public string AuthorEmail { get; set; } = string.Empty;

    [JsonPropertyName("authored_date")]
    public DateTime AuthoredDate { get; set; }

    [JsonPropertyName("committer_name")]
    public string CommitterName { get; set; } = string.Empty;

    [JsonPropertyName("committer_email")]
    public string CommitterEmail { get; set; } = string.Empty;

    [JsonPropertyName("committed_date")]
    public DateTime CommittedDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("parent_ids")]
    public List<string> ParentIds { get; set; } = new();

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("trailers")]
    public Dictionary<string, string> Trailers { get; set; } = new();

    [JsonPropertyName("extended_trailers")]
    public Dictionary<string, List<string>> ExtendedTrailers { get; set; } = new();
}
