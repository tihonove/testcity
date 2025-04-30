using System.Text.Json;
using Kontur.TestCity.Core.GitlabProjects;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.Storage;

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
                UseHooks = true
            })
            .ToList();

        var groups = groupEntities.Values
            .Select(g =>
            {
                var groupParams = string.IsNullOrEmpty(g.ParamsJson)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(g.ParamsJson,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                      ?? new Dictionary<string, object>();

                return new GitLabGroup
                {
                    Id = g.Id.ToString(),
                    Title = g.Title,
                    MergeRunsFromJobs = groupParams.TryGetValue("mergeRunsFromJobs", out var merge) && merge is JsonElement element
                        ? element.GetBoolean()
                        : false,
                    Projects = new List<GitLabProject>(),
                    Groups = new List<GitLabGroup>()
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
                    group.Projects ??= new List<GitLabProject>();
                    group.Projects.Add(project);
                }
            }
            else if (entity.Type == GitLabEntityType.Group && groupEntities.TryGetValue(entity.ParentId.Value, out var parentGroupEntity))
            {
                if (groups.TryGetValue(entity.Id, out var childGroup) &&
                    groups.TryGetValue(parentGroupEntity.Id, out var parentGroup2))
                {
                    parentGroup2.Groups ??= new List<GitLabGroup>();
                    parentGroup2.Groups.Add(childGroup);
                }
            }
        }

        return groups.Values
            .Where(g => !allEntities.Any(e => e.Id == long.Parse(g.Id) && e.ParentId != null))
            .ToList();
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
