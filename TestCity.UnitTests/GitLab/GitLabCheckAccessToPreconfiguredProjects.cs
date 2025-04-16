using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitLab.Models;
using Kontur.TestCity.Core.Logging;
using Microsoft.Extensions.Logging;
using NGitLab;
using NGitLab.Models;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.GitLab;

[TestFixture]
public class GitLabCheckAccessToPreconfiguredProjects
{
    [SetUp]
    public void SetUp()
    {
        var provider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        clientEx = provider.GetExtendedClient();
        client = provider.GetClient();
    }

    [TearDown]
    public void TearDown()
    {
        clientEx.Dispose();
    }

    [Test]
    public async Task CheckAccessToProject()
    {
        logger = GlobalSetup.TestLoggerFactory.CreateLogger<GitLabCheckAccessToPreconfiguredProjects>();
        var allProjects = Core.GitlabProjects.PreconfiguredGitLabProjectsService.GetAllProjects();
        foreach (var project in allProjects)
        {
            try
            {
                var projectId = long.Parse(project.Id);
                var projectInfo = client.Projects.GetById(projectId, new SingleProjectQuery());
                var pipelines = client.GetPipelines(projectId).All.Take(10).ToList();
                if (pipelines.Count == 0)
                    continue;

                var jobs = new List<GitLabJob>();
                await foreach (var job in clientEx.GetAllProjectJobsAsync(projectId))
                {
                    jobs.Add(job);
                    if (jobs.Count >= 1)
                        break;
                }
                var firstJob = jobs.First();

                if (!string.IsNullOrEmpty(firstJob.Commit?.Id))
                {
                    var commit = client.GetCommits(projectId).GetCommit(firstJob.Commit.Id);
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Cannot access to {project.Title}");
            }
        }
    }

    private ILogger logger = Log.GetLog<GitLabCheckAccessToPreconfiguredProjects>();
    private GitLabExtendedClient clientEx;
    private IGitLabClient client;
}
