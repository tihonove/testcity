namespace TestCity.Api.Models;

public abstract class EntityNodeDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Type { get; set; } = null!;
}

public class GroupEntityShoriInfoNodeDto : EntityNodeDto
{
    public GroupEntityShoriInfoNodeDto()
    {
        Type = "group";
    }
}

public class GroupEntityNodeDto : GroupEntityShoriInfoNodeDto
{
    public GroupEntityNodeDto()
    {
        Type = "group";
    }

    public List<GroupEntityNodeDto> Groups { get; set; } = [];
    public List<ProjectEntityNodeDto> Projects { get; set; } = [];
}

public class ProjectEntityNodeDto : EntityNodeDto
{
    public ProjectEntityNodeDto()
    {
        Type = "project";
    }
}
