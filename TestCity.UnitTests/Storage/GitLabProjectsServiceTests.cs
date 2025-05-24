using TestCity.Core.Clickhouse;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;
using NUnit.Framework;
using TestCity.Core.GitLab;

namespace TestCity.UnitTests.Storage;

[TestFixture]
public sealed class GitLabProjectsServiceTests : IDisposable
{
    [SetUp]
    public async Task SetUp()
    {
        var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        database = new TestCityDatabase(connectionFactory);
        service = new GitLabProjectsService(database, clientProvider);
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    [TearDown]
    public void TearDown()
    {
        database.GitLabEntities.DeleteById(100, 1001, 1002, 1003, 2001, 2002, 3001);
        service?.Dispose();
    }

    [Test]
    public async Task SaveGitLabHierarchy_RetrievesHierarchy_CorrectlyPreservesStructure()
    {
        // Arrange
        var rootGroups = CreateTestHierarchy();

        // Act
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);
        var retrievedRootGroups = await service.GetRootGroupsInfo(CancellationToken.None);
        var retrievedGroup = await service.GetGroup("100", CancellationToken.None);
        var allProjects = await service.GetAllProjects(CancellationToken.None);

        // Assert
        Assert.That(retrievedRootGroups, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(retrievedRootGroups.First(x => x.Id == "100").Id, Is.EqualTo("100"));
        Assert.That(retrievedRootGroups.First(x => x.Id == "100").Title, Is.EqualTo("Root Group"));

        Assert.That(retrievedGroup, Is.Not.Null);
        Assert.That(retrievedGroup!.Title, Is.EqualTo("Root Group"));
        Assert.That(retrievedGroup.Groups, Has.Count.EqualTo(2));
        Assert.That(retrievedGroup.Projects, Has.Count.GreaterThanOrEqualTo(2));

        Assert.That(allProjects, Has.Count.GreaterThanOrEqualTo(5));

        var projectIds = allProjects.Select(p => p.Id).ToHashSet();
        Assert.That(projectIds, Contains.Item("1001"));
        Assert.That(projectIds, Contains.Item("1002"));
        Assert.That(projectIds, Contains.Item("2001"));
        Assert.That(projectIds, Contains.Item("2002"));
        Assert.That(projectIds, Contains.Item("3001"));

        var child1 = retrievedGroup.Groups.FirstOrDefault(g => g.Id == "200");
        Assert.That(child1, Is.Not.Null);
        Assert.That(child1!.Title, Is.EqualTo("Child Group 1"));
        Assert.That(child1.Projects, Has.Count.EqualTo(2));

        var child2 = retrievedGroup.Groups.FirstOrDefault(g => g.Id == "300");
        Assert.That(child2, Is.Not.Null);
        Assert.That(child2!.Title, Is.EqualTo("Child Group 2"));
        Assert.That(child2.Projects, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SaveGitLabHierarchy_UpdatesExistingEntities_WithNewData()
    {
        // Arrange
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);

        // Modify hierarchy
        var updatedRootGroups = CreateTestHierarchy();
        updatedRootGroups[0].Title = "Updated Root Group";
        updatedRootGroups[0].Groups[0].Title = "Updated Child Group 1";

        // Add new project to child group
        var newProject = new GitLabProject
        {
            Id = "1003",
            Title = "New Project"
        };
        updatedRootGroups[0].Projects.Add(newProject);

        // Act
        await service.SaveGitLabHierarchy(updatedRootGroups, CancellationToken.None);
        var retrievedGroup = await service.GetGroup("100", CancellationToken.None);
        var allProjects = await service.GetAllProjects(CancellationToken.None);

        // Assert
        Assert.That(retrievedGroup, Is.Not.Null);
        Assert.That(retrievedGroup!.Title, Is.EqualTo("Updated Root Group"));

        var child1 = retrievedGroup.Groups.FirstOrDefault(g => g.Id == "200");
        Assert.That(child1, Is.Not.Null);
        Assert.That(child1!.Title, Is.EqualTo("Updated Child Group 1"));

        Assert.That(allProjects, Has.Count.GreaterThanOrEqualTo(6));

        var projectTitles = allProjects.ToDictionary(p => p.Id, p => p.Title);
        Assert.That(projectTitles, Contains.Key("1003"));
        Assert.That(projectTitles["1003"], Is.EqualTo("New Project"));
    }

    [Test]
    public async Task HasProject_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);

        // Act
        var hasProject = await service.HasProject(1001, CancellationToken.None);

        // Assert
        Assert.That(hasProject, Is.True);
    }

    [Test]
    public async Task HasProject_WithNonExistingId_ReturnsFalse()
    {
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);
        var hasProject = await service.HasProject(9999, CancellationToken.None);
        Assert.That(hasProject, Is.False);
    }

    [Test]
    public async Task EnumerateAllProjectsIds_ReturnsAllProjectIds()
    {
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);

        // Act
        var projectIdsEnumerable = service.EnumerateAllProjectsIds(CancellationToken.None);
        var projectIds = await projectIdsEnumerable.ToListAsync();

        // Assert
        Assert.That(projectIds, Contains.Item(1001));
        Assert.That(projectIds, Contains.Item(1002));
        Assert.That(projectIds, Contains.Item(2001));
        Assert.That(projectIds, Contains.Item(2002));
        Assert.That(projectIds, Contains.Item(3001));
    }

    private static List<GitLabGroup> CreateTestHierarchy()
    {
        var rootGroup = new GitLabGroup
        {
            Id = "100",
            Title = "Root Group",
            MergeRunsFromJobs = true,
            Groups = [
                new GitLabGroup
                {
                    Id = "200",
                    Title = "Child Group 1",
                    Groups = [],
                    Projects =
                    [
                        new GitLabProject
                        {
                            Id = "2001",
                            Title = "Child 1 Project 1"
                        },
                        new GitLabProject
                        {
                            Id = "2002",
                            Title = "Child 1 Project 2"
                        }
                    ]
                },
                new GitLabGroup
                {
                    Id = "300",
                    Title = "Child Group 2",
                    Projects =
                    [
                        new GitLabProject
                        {
                            Id = "3001",
                            Title = "Child 2 Project 1"
                        }
                    ]
                }
            ],
            Projects =
            [
                new GitLabProject
                {
                    Id = "1001",
                    Title = "Root Project 1",
                },
                new GitLabProject
                {
                    Id = "1002",
                    Title = "Root Project 2"
                }
            ]
        };

        return [rootGroup];
    }

    private ConnectionFactory connectionFactory = null!;
    private TestCityDatabase database = null!;
    private GitLabProjectsService service = null!;

    public void Dispose()
    {
        service?.Dispose();
    }
}
