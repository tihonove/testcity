using System.Text.Json;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage.DTO;

namespace TestCity.Core.Storage;

public static class GitLabEntityRecordExtensions
{
    public async static Task<List<GitLabGroup>> ToGitLabGroups(this IAsyncEnumerable<GitLabEntityRecord> gitLabEntityRecords, CancellationToken cancellationToken = default)
    {
        var allEntities = await gitLabEntityRecords.ToListAsync();

        var groupEntities = allEntities.Where(e => e.Type == GitLabEntityType.Group).ToDictionary(e => e.Id);
        var projectEntities = allEntities.Where(e => e.Type == GitLabEntityType.Project).ToDictionary(e => e.Id);

    
        var projects = projectEntities.Values
            .Select(p => new GitLabProject
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                UseHooks = ReadParams(p.ParamsJson).TryGetValue("useHooks", out var useHooks) && useHooks is JsonElement element
                    ? element.GetBoolean()
                    : true,
            })
            .ToList();

        var groups = groupEntities.Values
            .Select(g =>
            {
                var paramsJson = g.ParamsJson;
                var groupParams = ReadParams(paramsJson);

                return new GitLabGroup
                {
                    Id = g.Id.ToString(),
                    Title = g.Title,
                    MergeRunsFromJobs = groupParams.TryGetValue("mergeRunsFromJobs", out var merge) && merge is JsonElement element
                        ? element.GetBoolean()
                        : false,
                    Projects = [],
                    Groups = []
                };
            })
            .ToDictionary(g => long.Parse(g.Id));

        foreach (var entity in allEntities)
        {
            if (entity.ParentId is null)
                continue;

            if (entity.Type == GitLabEntityType.Project && groupEntities.TryGetValue(entity.ParentId.Value, out var parentGroup))
            {
                var project = projects.FirstOrDefault(p => p.Id == entity.Id.ToString());
                if (project != null && groups.TryGetValue(parentGroup.Id, out var group))
                {
                    group.Projects ??= [];
                    group.Projects.Add(project);
                }
            }
            else if (entity.Type == GitLabEntityType.Group && groupEntities.TryGetValue(entity.ParentId.Value, out var parentGroupEntity))
            {
                if (groups.TryGetValue(entity.Id, out var childGroup) &&
                    groups.TryGetValue(parentGroupEntity.Id, out var parentGroup2))
                {
                    parentGroup2.Groups ??= [];
                    parentGroup2.Groups.Add(childGroup);
                }
            }
        }

        return groups.Values
            .Where(g => !allEntities.Any(e => e.Id == long.Parse(g.Id) && e.ParentId != null))
            .ToList();
    }

    private static Dictionary<string, object> ReadParams(string paramsJson)
    {
        return string.IsNullOrEmpty(paramsJson)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? [];
    }

    public static IEnumerable<GitLabEntityRecord> ToGitLabEntityRecords(this IEnumerable<GitLabGroup> groups, long? parentId)
    {
        foreach (var group in groups)
        {
            var groupParams = new Dictionary<string, object>();
            if (group.MergeRunsFromJobs.HasValue)
            {
                groupParams["mergeRunsFromJobs"] = group.MergeRunsFromJobs;
            }
            yield return new GitLabEntityRecord
            {
                Id = long.Parse(group.Id),
                Type = GitLabEntityType.Group,
                Title = group.Title,
                ParentId = parentId,
                ParamsJson = JsonSerializer.Serialize(groupParams, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            };

            foreach (var project in group.Projects ?? [])
            {
                yield return new GitLabEntityRecord
                {
                    Id = long.Parse(project.Id),
                    Type = GitLabEntityType.Project,
                    Title = project.Title,
                    ParentId = long.Parse(group.Id),
                    ParamsJson = project.UseHooks == false ? JsonSerializer.Serialize(new Dictionary<string, object>() { { "useHooks", false } }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) : string.Empty,
                };
            }

            foreach (var subGroup in (group.Groups ?? []).ToGitLabEntityRecords(long.Parse(group.Id)))
            {
                yield return subGroup;
            }
        }
    }
}
