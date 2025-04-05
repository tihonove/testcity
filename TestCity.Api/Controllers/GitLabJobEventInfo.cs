namespace Kontur.TestCity.Api.Controllers;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GitLabJobEventBuildStatus
{
    [EnumMember(Value = "unknown")]
    Unknown,

    [EnumMember(Value = "running")]
    Running,

    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "success")]
    Success,

    [EnumMember(Value = "created")]
    Created,

    [EnumMember(Value = "canceled")]
    Canceled,

    [EnumMember(Value = "skipped")]
    Skipped,

    [EnumMember(Value = "manual")]
    Manual,

    [EnumMember(Value = "no_build")]
    NoBuild,

    [EnumMember(Value = "preparing")]
    Preparing,

    [EnumMember(Value = "waiting_for_resource")]
    WaitingForResource,

    [EnumMember(Value = "scheduled")]
    Scheduled,

    [EnumMember(Value = "canceling")]
    Canceling,
}


public class GitLabJobEventInfo
{
    [JsonPropertyName("object_kind")]
    public string? ObjectKind { get; set; }

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("tag")]
    public bool Tag { get; set; }

    [JsonPropertyName("before_sha")]
    public string? BeforeSha { get; set; }

    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    [JsonPropertyName("retries_count")]
    public int RetriesCount { get; set; }

    [JsonPropertyName("build_id")]
    public long BuildId { get; set; }

    [JsonPropertyName("build_name")]
    public string? BuildName { get; set; }

    [JsonPropertyName("build_stage")]
    public string? BuildStage { get; set; }

    [JsonPropertyName("build_status")]
    public GitLabJobEventBuildStatus? BuildStatus { get; set; }

    [JsonPropertyName("build_created_at")]
    public string? BuildCreatedAt { get; set; }

    [JsonPropertyName("build_started_at")]
    public string? BuildStartedAt { get; set; }

    [JsonPropertyName("build_finished_at")]
    public string? BuildFinishedAt { get; set; }

    [JsonPropertyName("build_duration")]
    public double BuildDuration { get; set; }

    [JsonPropertyName("build_queued_duration")]
    public double BuildQueuedDuration { get; set; }

    [JsonPropertyName("build_allow_failure")]
    public bool BuildAllowFailure { get; set; }

    [JsonPropertyName("build_failure_reason")]
    public string? BuildFailureReason { get; set; }

    [JsonPropertyName("pipeline_id")]
    public long PipelineId { get; set; }

    [JsonPropertyName("runner")]
    public RunnerInfo? Runner { get; set; }

    [JsonPropertyName("project_id")]
    public long ProjectId { get; set; }

    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }

    [JsonPropertyName("commit")]
    public CommitInfo? Commit { get; set; }

    [JsonPropertyName("repository")]
    public RepositoryInfo? Repository { get; set; }

    [JsonPropertyName("project")]
    public ProjectInfo? Project { get; set; }

    [JsonPropertyName("environment")]
    public object? Environment { get; set; }

    public class RunnerInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("runner_type")]
        public string? RunnerType { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("is_shared")]
        public bool IsShared { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class CommitInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("sha")]
        public string? Sha { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("author_name")]
        public string? AuthorName { get; set; }

        [JsonPropertyName("author_email")]
        public string? AuthorEmail { get; set; }

        [JsonPropertyName("author_url")]
        public string? AuthorUrl { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("started_at")]
        public string? StartedAt { get; set; }

        [JsonPropertyName("finished_at")]
        public string? FinishedAt { get; set; }
    }

    public class RepositoryInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("git_http_url")]
        public string? GitHttpUrl { get; set; }

        [JsonPropertyName("git_ssh_url")]
        public string? GitSshUrl { get; set; }

        [JsonPropertyName("visibility_level")]
        public int VisibilityLevel { get; set; }
    }

    public class ProjectInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("web_url")]
        public string? WebUrl { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("git_ssh_url")]
        public string? GitSshUrl { get; set; }

        [JsonPropertyName("git_http_url")]
        public string? GitHttpUrl { get; set; }

        [JsonPropertyName("namespace")]
        public string? Namespace { get; set; }

        [JsonPropertyName("visibility_level")]
        public int VisibilityLevel { get; set; }

        [JsonPropertyName("path_with_namespace")]
        public string? PathWithNamespace { get; set; }

        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        [JsonPropertyName("ci_config_path")]
        public string? CiConfigPath { get; set; }
    }
}
