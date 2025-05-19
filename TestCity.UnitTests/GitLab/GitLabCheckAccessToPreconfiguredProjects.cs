using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitLab.Models;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using Microsoft.Extensions.Logging;
using NGitLab;
using NGitLab.Models;
using NUnit.Framework;
using TestCity.UnitTests.Utils;

namespace TestCity.UnitTests.GitLab;

[TestFixture]
public class GitLabCheckAccessToPreconfiguredProjects
{
    [SetUp]
    public async Task SetUp()
    {
        CIUtils.SkipOnGitHubActions();
        var provider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        clientEx = provider.GetExtendedClient();
        client = provider.GetClient();
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    [TearDown]
    public void TearDown()
    {
        clientEx?.Dispose();
    }

    [Test]
    public async Task CheckAccessToProject()
    {
        logger = GlobalSetup.TestLoggerFactory.CreateLogger<GitLabCheckAccessToPreconfiguredProjects>();
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var gitLabProjectsService = new GitLabProjectsService(database);
        var allProjects = await gitLabProjectsService.GetAllProjects();
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
                logger.LogError(e, $"Cannot access to {project.Title}");
            }
        }
    }

    private ILogger logger = Log.GetLog<GitLabCheckAccessToPreconfiguredProjects>();
    private GitLabExtendedClient clientEx;
    private IGitLabClient client;
}
