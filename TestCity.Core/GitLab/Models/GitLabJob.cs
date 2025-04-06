using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Kontur.TestCity.Core.GitLab.Models;

public class GitLabJob
{
    [JsonPropertyName("commit")]
    public GitLabCommit? Commit { get; set; }

    [JsonPropertyName("coverage")]
    public double? Coverage { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("allow_failure")]
    public bool AllowFailure { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [JsonPropertyName("erased_at")]
    public DateTime? ErasedAt { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("queued_duration")]
    public double? QueuedDuration { get; set; }

    [JsonPropertyName("artifacts_file")]
    public ArtifactFile? ArtifactsFile { get; set; }

    [JsonPropertyName("artifacts")]
    public List<Artifact>? Artifacts { get; set; }

    [JsonPropertyName("artifacts_expire_at")]
    public DateTime? ArtifactsExpireAt { get; set; }

    [JsonPropertyName("tag_list")]
    public List<string>? TagList { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("pipeline")]
    public GitLabPipeline? Pipeline { get; set; }

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("runner")]
    public GitLabRunner? Runner { get; set; }

    [JsonPropertyName("runner_manager")]
    public GitLabRunnerManager? RunnerManager { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("tag")]
    public bool Tag { get; set; }

    [JsonPropertyName("web_url")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("project")]
    public GitLabProject? Project { get; set; }

    [JsonPropertyName("user")]
    public GitLabUser? User { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
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

    [EnumMember(Value = "nobuild")]
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


public class GitLabCommit
{
    [JsonPropertyName("author_email")]
    public string? AuthorEmail { get; set; }

    [JsonPropertyName("author_name")]
    public string? AuthorName { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("short_id")]
    public string? ShortId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class ArtifactFile
{
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public class Artifact
{
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("file_format")]
    public string? FileFormat { get; set; }
}

public class GitLabPipeline
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("project_id")]
    public int ProjectId { get; set; }

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("web_url")]
    public string? WebUrl { get; set; }
}

public class GitLabRunner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("is_shared")]
    public bool IsShared { get; set; }

    [JsonPropertyName("runner_type")]
    public string? RunnerType { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("online")]
    public bool Online { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class GitLabRunnerManager
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("system_id")]
    public string? SystemId { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("revision")]
    public string? Revision { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    [JsonPropertyName("architecture")]
    public string? Architecture { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("contacted_at")]
    public DateTime ContactedAt { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class GitLabProject
{
    [JsonPropertyName("ci_job_token_scope_enabled")]
    public bool CiJobTokenScopeEnabled { get; set; }
}

public class GitLabUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("web_url")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("public_email")]
    public string? PublicEmail { get; set; }

    [JsonPropertyName("skype")]
    public string? Skype { get; set; }

    [JsonPropertyName("linkedin")]
    public string? Linkedin { get; set; }

    [JsonPropertyName("twitter")]
    public string? Twitter { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }
}
