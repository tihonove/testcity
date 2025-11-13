using TestCity.Api.Models;
using TestCity.Core.GitlabProjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Reflection.Metadata.Ecma335;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class GroupsController(GitLabProjectsService gitLabProjectsService) : ControllerBase
{
    [HttpGet("groups")]
    public async Task<ActionResult<List<GroupDto>>> GetRootGroups() => Ok((await gitLabProjectsService.GetRootGroupsInfo()).ConvertAll(MapToGroupDto));

    [HttpGet("groups-v2")]
    public async Task<ActionResult<List<GroupEntityShoriInfoNodeDto>>> GetRootGroupsV2() => Ok((await gitLabProjectsService.GetRootGroupsInfo()).ConvertAll(MapToGroupV2));

    [HttpGet("groups/{idOrTitle}")]
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
            AvatarUrl = group.AvatarUrl
        };
    }

    private static GroupEntityShoriInfoNodeDto MapToGroupV2(GitLabGroupShortInfo group)
    {
        return new GroupEntityNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            AvatarUrl = group.AvatarUrl,
        };
    }

    private static GroupNodeDto MapToGroupNodeDto(GitLabGroup group)
    {
        return new GroupNodeDto
        {
            Id = group.Id,
            Title = group.Title,
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
