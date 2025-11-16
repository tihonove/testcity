using TestCity.Api.Models;
using TestCity.Core.GitlabProjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Reflection.Metadata.Ecma335;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class GroupsController(GitLabPathResolver gitLabPathResolver) : ControllerBase
{
    [HttpGet("groups-v2")]
    public async Task<ActionResult<List<GroupEntityShoriInfoNodeDto>>> GetRootGroupsV2() => Ok((await gitLabPathResolver.GetRootGroupsInfo()).ConvertAll(MapToGroupV2));
    
    private static GroupEntityShoriInfoNodeDto MapToGroupV2(GitLabGroupShortInfo group)
    {
        return new GroupEntityNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            AvatarUrl = group.AvatarUrl,
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
