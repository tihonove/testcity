using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitlabProjects;
using Kontur.TestCity.Core.Storage;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.Explicits;

public class PreconfiguredGitLabProjectsServiceTests
{
    [Test]
    public void TestOutputRootGroups()
    {
        foreach (var group in PreconfiguredGitLabProjectsService.Projects)
        {
            Logger.LogInformation("Group ID: {GroupId}, Title: {Title}", group.Id, group.Title);
        }
    }

    [Test]
    public void TestGetAllProjects()
    {
        Logger.LogInformation("Starting TestGetAllProjects...");
        foreach (var project in PreconfiguredGitLabProjectsService.GetAllProjects())
        {
            Logger.LogInformation("Project ID: {ProjectId}, Title: {Title}", project.Id, project.Title);
        }
    }

    private static readonly ILoggerFactory LoggerFactory = GlobalSetup.TestLoggerFactory;
    private static readonly ILogger<PreconfiguredGitLabProjectsServiceTests> Logger = LoggerFactory.CreateLogger<PreconfiguredGitLabProjectsServiceTests>();
}
