using TestCity.Core.GitLab;

namespace TestCity.Core.GitlabProjects;

public class GitLabPathResolver(GitLabProjectsService gitLabProjectsService)
{
    public async Task<List<GitLabEntity>> ResolveGroupOrProjectPath(string[] groupIdOrTitles)
    {
        var result = new List<GitLabEntity>();
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

    public async Task<GitLabProject[]> ResolveProjects(string[] groupIdOrTitles)
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

    private IEnumerable<GitLabProject> GetAllProjectsRecursive(GitLabGroup group)
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
