namespace TestCity.Api.Models;

public class GroupDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public bool? MergeRunsFromJobs { get; set; }
    public string? AvatarUrl { get; internal set; }
}

public class GroupNodeDto : GroupDto
{
    public List<GroupNodeDto>? Groups { get; set; }
    public List<ProjectDto>? Projects { get; set; }
}

public class ProjectDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public bool? UseHooks { get; set; }
    public string? AvatarUrl { get; internal set; }
}
