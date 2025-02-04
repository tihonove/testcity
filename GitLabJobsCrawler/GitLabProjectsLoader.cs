using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

public static class GitLabProjectsLoader
{
    private static readonly List<GitLabGroup> projects;

    static GitLabProjectsLoader()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Kontur.TestAnalytics.GitLabJobsCrawler.gitlab-projects.json";

        using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found");
        using StreamReader reader = new(stream);
        string json = reader.ReadToEnd();
        projects = JsonSerializer.Deserialize<List<GitLabGroup>>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new Exception("Failed to deserialize json");
    }

    public static List<GitLabGroup> GetGroups()
    {
        return projects;
    }

    public static List<string> GetAllProjectIds()
    {
        var projectIds = new List<string>();
        foreach (var group in projects)
        {
            GetProjectIdsRecursive(group, projectIds);
        }
        return projectIds;
    }

    private static void GetProjectIdsRecursive(GitLabGroup group, List<string> projectIds)
    {
        if (group.Projects != null)
        {
            foreach (var project in group.Projects)
            {
                projectIds.Add(project.Id);
            }
        }

        if (group.Groups != null)
        {
            foreach (var subGroup in group.Groups)
            {
                GetProjectIdsRecursive(subGroup, projectIds);
            }
        }
    }
}
