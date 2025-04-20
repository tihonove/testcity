using Kontur.TestCity.Api.Models;
using Kontur.TestCity.Core.GitlabProjects;
using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestCity.Api.Controllers;

/// <summary>
/// Контроллер для получения информации о группах и проектах
/// </summary>
[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    /// <summary>
    /// Получить список всех корневых групп
    /// </summary>
    [HttpGet("")]
    public ActionResult<List<GroupDto>> GetRootGroups()
    {
        var groups = PreconfiguredGitLabProjectsService.Projects;
        var groupDtos = groups.Select(MapToGroupDto).ToList();

        return Ok(groupDtos);
    }

    /// <summary>
    /// Получить группу по идентификатору или названию
    /// </summary>
    /// <param name="idOrTitle">Идентификатор или название группы</param>
    [HttpGet("{idOrTitle}")]
    public ActionResult<GroupNodeDto> GetGroup(string idOrTitle)
    {
        var group = PreconfiguredGitLabProjectsService.Projects
            .FirstOrDefault(g => g.Id == idOrTitle || g.Title.Equals(idOrTitle, StringComparison.OrdinalIgnoreCase));

        if (group == null)
            return NotFound($"Группа с идентификатором или названием '{idOrTitle}' не найдена");

        var groupDto = MapToGroupNodeDto(group);
        return Ok(groupDto);
    }

    private static GroupDto MapToGroupDto(GitLabGroup group)
    {
        return new GroupDto
        {
            Id = group.Id,
            Title = group.Title,
            MergeRunsFromJobs = GetMergeRunsFromJobsValue(group)
        };
    }

    private static bool? GetMergeRunsFromJobsValue(GitLabGroup group)
    {
        // Получаем значение из свойства MergeRunsFromJobs с помощью рефлексии, 
        // так как оно может отсутствовать в модели GitLabGroup
        var property = group.GetType().GetProperty("MergeRunsFromJobs");
        if (property != null)
        {
            var value = property.GetValue(group);
            if (value != null)
                return (bool)value;
        }
        return null;
    }

    private static GroupNodeDto MapToGroupNodeDto(GitLabGroup group)
    {
        return new GroupNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            MergeRunsFromJobs = GetMergeRunsFromJobsValue(group),
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
            UseHooks = project.UseHooks
        };
    }
}
