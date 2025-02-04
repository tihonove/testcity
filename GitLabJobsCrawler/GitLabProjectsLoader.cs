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

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            string json = reader.ReadToEnd();
            projects = JsonSerializer.Deserialize<List<GitLabGroup>>(json);
        }
    }

    public static List<GitLabGroup> GetGroups()
    {
        return projects;
    }
}
