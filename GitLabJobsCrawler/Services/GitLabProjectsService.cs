using System.Reflection;
using System.Text.Json;
using Kontur.TestAnalytics.GitLabJobsCrawler.Models;

namespace Kontur.TestAnalytics.GitLabJobsCrawler.Services
{
    public static class GitLabProjectsService
    {
        private static readonly Lazy<List<GitLabGroup>> _projects = new Lazy<List<GitLabGroup>>(LoadProjects);

        public static List<GitLabGroup> Projects => _projects.Value;

        private static List<GitLabGroup> LoadProjects()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Kontur.TestAnalytics.GitLabJobsCrawler.gitlab-projects.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Resource '{resourceName}' not found.");
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var projects = JsonSerializer.Deserialize<List<GitLabGroup>>(json, options);
            if (projects == null)
            {
                throw new InvalidOperationException("Failed to deserialize GitLab projects.");
            }

            return projects;
        }

        public static List<GitLabProject> GetAllProjects()
        {
            var allProjects = new List<GitLabProject>();
            foreach (var group in Projects)
            {
                TraverseGroup(group, allProjects);
            }
            return allProjects;
        }

        private static void TraverseGroup(GitLabGroup group, List<GitLabProject> allProjects)
        {
            if (group.Projects != null)
            {
                allProjects.AddRange(group.Projects);
            }

            if (group.Groups != null)
            {
                foreach (var subGroup in group.Groups)
                {
                    TraverseGroup(subGroup, allProjects);
                }
            }
        }
    }
}
