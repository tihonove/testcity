using System.Collections.Generic;

namespace TestCity.SystemTests.Api;

// Models from TestCity.Api.Models required for API testing
public class GroupDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool MergeRunsFromJobs { get; set; }
}

public class GroupNodeDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool MergeRunsFromJobs { get; set; }
    public List<GroupNodeDto>? Groups { get; set; }
    public List<ProjectDto>? Projects { get; set; }
}

public class ProjectDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool UseHooks { get; set; }
}

public class GitLabJobEventInfo
{
    public long ProjectId { get; set; }
    public long BuildId { get; set; }
    public string BuildStatus { get; set; } = null!;
}

public class ProcessJobRunTaskPayload
{
    public long ProjectId { get; set; }
    public long JobRunId { get; set; }
}

public class ProcessInProgressJobTaskPayload
{
    public long ProjectId { get; set; }
    public long JobRunId { get; set; }
}

public enum ManualJobRunStatus
{
    Manual,
    Susccess,
    Failed
}

public class ManualJobRunInfo
{
    public string JobId { get; set; } = null!;
    public string JobRunId { get; set; } = null!;
    public ManualJobRunStatus Status { get; set; }
}
