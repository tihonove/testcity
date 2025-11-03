using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/groups-v2/{groupPath1}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}/{groupPath4}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}/{groupPath4}/{groupPath5}")]
public class TestRunsContoller : ControllerBase
{
    private readonly GitLabProjectsService gitLabProjectsService;
    private readonly TestCityDatabase database;

    public TestRunsContoller(GitLabProjectsService gitLabProjectsService, TestCityDatabase database)
    {
        this.gitLabProjectsService = gitLabProjectsService;
        this.database = database;
    }

    [HttpGet("branches")]
    public async Task<ActionResult<string[]>> FindAllBranches([FromQuery] string? jobId = null)
    {
        var projects = await ResolveProjectsFromContext();
        return Ok(await database.FindBranches([.. projects.Select(p => p.Id)], jobId));
    }

    private async Task<GitLabProject[]> ResolveProjectsFromContext()
    {
        var groupSegments = RouteData.Values
            .Where(kv => kv.Key.StartsWith("groupPath"))
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value?.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        return await ResolveProjects(groupSegments);
    }

    private async Task<GitLabProject[]> ResolveProjects(string[] groupIdOrTitles)
    {
        GitLabGroup? currentGroup = null;
        for (int i = 0; i < groupIdOrTitles.Length; i++)
        {
            var idOrTitle = groupIdOrTitles[i];
            if (currentGroup == null)
            {
                currentGroup = await gitLabProjectsService.GetGroup(idOrTitle);
            }
            else
            {
                var nextGroup = currentGroup.Groups.FirstOrDefault(g => g.Id.ToString() == idOrTitle || g.Title == idOrTitle);
                if (nextGroup == null && i == groupIdOrTitles.Length - 1)
                {
                    var project = currentGroup.Projects.FirstOrDefault(p => p.Id == idOrTitle || p.Title == idOrTitle);
                    if (project != null)
                    {
                        return [project];
                    }
                }
                currentGroup = nextGroup;
            }

            if (currentGroup == null)
                throw new Exception($"Группа с идентификатором или названием '{idOrTitle}' не найдена");
        }

        if (currentGroup == null)
            return [];

        return GetAllProjectsRecursive(currentGroup).ToArray();
    }

    private static IEnumerable<GitLabProject> GetAllProjectsRecursive(GitLabGroup group)
    {
        foreach (var project in group.Projects ?? [])
        {
            yield return project;
        }
        foreach (var childGroup in group.Groups ?? [])
        {
            foreach (var project in GetAllProjectsRecursive(childGroup))
            {
                yield return project;
            }
        }
    }
}

public class JobsDashboard
{
    public GroupNode[] Groups { get; set; }

    public class GroupNode
    {
        public string Title { get; set; }
        public GroupNode[] Groups { get; set; }
        public JobNode[] Jobs { get; set; }
    }

    public class JobNode
    {
        
    }
}
