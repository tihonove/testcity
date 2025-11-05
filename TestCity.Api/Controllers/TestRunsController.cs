using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestCity.Api.Models;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/groups-v2/{groupPath1}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}/{groupPath4}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}/{groupPath4}/{groupPath5}")]
public class TestRunsContoller(GitLabProjectsService gitLabProjectsService, TestCityDatabase database, GitLabSettings gitLabSettings) : ControllerBase
{    
    [HttpGet("branches")]
    public async Task<ActionResult<string[]>> FindAllBranches([FromQuery] string? jobId = null)
    {
        var projects = await ResolveProjectsFromContext();
        return Ok(await database.FindBranches([.. projects.Select(p => p.Id)], jobId));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardNodeDto>> GetDashboardData([FromQuery] string? branchName = null)
    {
        var groupOrProjectPath = await ResolveGroupOrProjectPathFromContext();
        var projects = GetAllProjectsRecursive(groupOrProjectPath).ToArray();
        var projectIds = projects.Select(p => p.Id).ToArray();

        var allJobs = await database.FindAllJobs(projectIds);
        var inProgressJobRuns = await database.FindAllJobsRunsInProgress(projectIds, branchName);
        var allJobRuns = await database.FindAllJobsRuns(projectIds, branchName);

        var result = BuildDashboardData(groupOrProjectPath, allJobs, inProgressJobRuns, allJobRuns);
        return Ok(result);
    }

    private DashboardNodeDto BuildDashboardData(
        List<object> groupOrProjectPath,
        JobIdWithParentProject[] allJobs,
        JobRunQueryResult[] inProgressJobRuns,
        JobRunQueryResult[] allJobRuns)
    {
        var currentNode = groupOrProjectPath[^1];

        if (currentNode is GitLabProject project)
        {
            return BuildProjectDashboardData(groupOrProjectPath, project, allJobs, inProgressJobRuns, allJobRuns);
        }
        else if (currentNode is GitLabGroup group)
        {
            return BuildGroupDashboardData(groupOrProjectPath, group, allJobs, inProgressJobRuns, allJobRuns);
        }

        throw new InvalidOperationException("Unknown node type");
    }

    private ProjectDashboardNodeDto BuildProjectDashboardData(
        List<object> groupOrProjectPath,
        GitLabProject project,
        JobIdWithParentProject[] allJobs,
        JobRunQueryResult[] inProgressJobRuns,
        JobRunQueryResult[] allJobRuns)
    {
        var projectJobs = allJobs.Where(j => j.ProjectId == project.Id).ToArray();
        var jobsWithRuns = projectJobs.Select(job =>
        {
            var jobRuns = inProgressJobRuns.Concat(allJobRuns)
                .Where(x => x.JobId == job.JobId && x.ProjectId == job.ProjectId)
                .ToList();

            return new JobDashboardInfoDto
            {
                JobId = job.JobId,
                Runs = jobRuns
            };
        }).ToList();

        return new ProjectDashboardNodeDto
        {
            Id = project.Id,
            Title = project.Title,
            AvatarUrl = project.AvatarUrl,
            Type = "project",
            Link = $"/api/groups-v2/{string.Join("/", groupOrProjectPath.Select(GetNodeId))}",
            GitLabLink = new Uri(gitLabSettings.Url, string.Join("/", groupOrProjectPath.Select(GetNodePathItem))).ToString(),
            FullPathSlug = groupOrProjectPath.Select(CreatePathSlugItem).ToList(),
            Jobs = jobsWithRuns
        };
    }

    private GroupDashboardNodeDto BuildGroupDashboardData(
        List<object> groupOrProjectPath,
        GitLabGroup group,
        JobIdWithParentProject[] allJobs,
        JobRunQueryResult[] inProgressJobRuns,
        JobRunQueryResult[] allJobRuns)
    {
        var children = new List<DashboardNodeDto>();

        foreach (var childProject in group.Projects)
        {
            var childPath = new List<object>(groupOrProjectPath) { childProject };
            children.Add(BuildProjectDashboardData(childPath, childProject, allJobs, inProgressJobRuns, allJobRuns));
        }

        foreach (var childGroup in group.Groups)
        {
            var childPath = new List<object>(groupOrProjectPath) { childGroup };
            children.Add(BuildGroupDashboardData(childPath, childGroup, allJobs, inProgressJobRuns, allJobRuns));
        }

        return new GroupDashboardNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            AvatarUrl = group.AvatarUrl,
            Type = "group",
            Link = $"/api/groups-v2/{string.Join("/", groupOrProjectPath.Select(GetNodeId))}",
            FullPathSlug = groupOrProjectPath.Select(CreatePathSlugItem).ToList(),
            Children = children
        };
    }

    private static string GetNodeId(object node)
    {
        return node switch
        {
            GitLabProject p => p.Id,
            GitLabGroup g => g.Id,
            _ => throw new InvalidOperationException("Unknown node type")
        };
    }

    private static string GetNodePathItem(object node)
    {
        return node switch
        {
            GitLabProject p => p.Title,
            GitLabGroup g => g.Title,
            _ => throw new InvalidOperationException("Unknown node type")
        };
    }

    private static GroupOrProjectPathSlugItem CreatePathSlugItem(object node)
    {
        return node switch
        {
            GitLabProject p => new GroupOrProjectPathSlugItem { Id = p.Id, Title = p.Title, AvatarUrl = p.AvatarUrl },
            GitLabGroup g => new GroupOrProjectPathSlugItem { Id = g.Id, Title = g.Title, AvatarUrl = g.AvatarUrl },
            _ => throw new InvalidOperationException("Unknown node type")
        };
    }

    private static IEnumerable<GitLabProject> GetAllProjectsRecursive(List<object> groupOrProjectPath)
    {
        var currentNode = groupOrProjectPath[^1];
        
        if (currentNode is GitLabProject project)
        {
            yield return project;
        }
        else if (currentNode is GitLabGroup group)
        {
            foreach (var p in group.Projects)
            {
                yield return p;
            }
            foreach (var childGroup in group.Groups)
            {
                var childPath = new List<object>(groupOrProjectPath) { childGroup };
                foreach (var p in GetAllProjectsRecursive(childPath))
                {
                    yield return p;
                }
            }
        }
    }

    private async Task<List<object>> ResolveGroupOrProjectPathFromContext()
    {
        var groupSegments = RouteData.Values
            .Where(kv => kv.Key.StartsWith("groupPath"))
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value?.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        return await ResolveGroupOrProjectPath(groupSegments!);
    }

    private async Task<List<object>> ResolveGroupOrProjectPath(string[] groupIdOrTitles)
    {
        var result = new List<object>();
        GitLabGroup? currentGroup = null;

        for (int i = 0; i < groupIdOrTitles.Length; i++)
        {
            var idOrTitle = groupIdOrTitles[i];
            if (currentGroup == null)
            {
                currentGroup = await gitLabProjectsService.GetGroup(idOrTitle);
                if (currentGroup == null)
                    throw new Exception($"Группа с идентификатором или названием '{idOrTitle}' не найдена");
                result.Add(currentGroup);
            }
            else
            {
                var nextGroup = currentGroup.Groups.FirstOrDefault(g => g.Id.ToString() == idOrTitle || g.Title == idOrTitle);
                if (nextGroup == null && i == groupIdOrTitles.Length - 1)
                {
                    var project = currentGroup.Projects.FirstOrDefault(p => p.Id == idOrTitle || p.Title == idOrTitle);
                    if (project != null)
                    {
                        result.Add(project);
                        return result;
                    }
                }
                
                if (nextGroup != null)
                {
                    currentGroup = nextGroup;
                    result.Add(nextGroup);
                }
                else
                {
                    throw new Exception($"Группа с идентификатором или названием '{idOrTitle}' не найдена");
                }
            }
        }

        return result;
    }

    private async Task<GitLabProject[]> ResolveProjectsFromContext()
    {
        var groupSegments = RouteData.Values
            .Where(kv => kv.Key.StartsWith("groupPath"))
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value?.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        return await ResolveProjects(groupSegments!);
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
