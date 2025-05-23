using TestCity.Api.Models;
using TestCity.Core.GitlabProjects;
using Microsoft.AspNetCore.Mvc;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupsController(GitLabProjectsService gitLabProjectsService) : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<List<GroupDto>>> GetRootGroups() => Ok((await gitLabProjectsService.GetRootGroupsInfo()).ConvertAll(MapToGroupDto));

    [HttpGet("{idOrTitle}")]
    public async Task<ActionResult<GroupNodeDto>> GetGroup(string idOrTitle)
    {
        var group = await gitLabProjectsService.GetGroup(idOrTitle);
        if (group == null)
            return NotFound($"Группа с идентификатором или названием '{idOrTitle}' не найдена");
        var groupDto = MapToGroupNodeDto(group);
        return Ok(groupDto);
    }

    private static GroupDto MapToGroupDto(GitLabGroupShortInfo group)
    {
        return new GroupDto
        {
            Id = group.Id,
            Title = group.Title,
            AvatarUrl = group.AvatarUrl,
            MergeRunsFromJobs = group.MergeRunsFromJobs
        };
    }

    private static GroupNodeDto MapToGroupNodeDto(GitLabGroup group)
    {
        return new GroupNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            MergeRunsFromJobs = group.MergeRunsFromJobs,
            AvatarUrl = group.AvatarUrl,
            Groups = group.Groups?.Select(MapToGroupNodeDto).ToList(),
            Projects = group.Projects?.Select(MapToProjectDto).ToList()
        };
    }

    private static ProjectDto MapToProjectDto(GitLabProject project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            AvatarUrl = project.AvatarUrl,
            UseHooks = project.UseHooks
        };
    }
}
