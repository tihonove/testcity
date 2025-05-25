using TestCity.Core.Clickhouse;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;
using Xunit;
using TestCity.Core.GitLab;
using Xunit.Abstractions;
using TestCity.UnitTests.Utils;

namespace TestCity.UnitTests.Storage;

[Collection("Global")]
public sealed class GitLabProjectsServiceTests : IAsyncLifetime, IAsyncDisposable
{
    public GitLabProjectsServiceTests(ITestOutputHelper output)
    {
        XUnitLoggerProvider.ConfigureTestLogger(output);
        var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        database = new TestCityDatabase(connectionFactory);
        service = new GitLabProjectsService(database, clientProvider);
    }

    public async Task InitializeAsync()
    {
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    public Task DisposeAsync()
    {
        database.GitLabEntities.DeleteById(100, 1001, 1002, 1003, 2001, 2002, 3001);
        service?.Dispose();
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }

    [Fact]
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
        Assert.True(retrievedRootGroups.Count >= 1);
        Assert.Equal("100", retrievedRootGroups.First(x => x.Id == "100").Id);
        Assert.Equal("Root Group", retrievedRootGroups.First(x => x.Id == "100").Title);

        Assert.NotNull(retrievedGroup);
        Assert.Equal("Root Group", retrievedGroup!.Title);
        Assert.Equal(2, retrievedGroup.Groups.Count);
        Assert.True(retrievedGroup.Projects.Count >= 2);

        Assert.True(allProjects.Count >= 5);

        var projectIds = allProjects.Select(p => p.Id).ToHashSet();
        Assert.Contains("1001", projectIds);
        Assert.Contains("1002", projectIds);
        Assert.Contains("2001", projectIds);
        Assert.Contains("2002", projectIds);
        Assert.Contains("3001", projectIds);

        var child1 = retrievedGroup.Groups.FirstOrDefault(g => g.Id == "200");
        Assert.NotNull(child1);
        Assert.Equal("Child Group 1", child1!.Title);
        Assert.Equal(2, child1.Projects.Count);

        var child2 = retrievedGroup.Groups.FirstOrDefault(g => g.Id == "300");
        Assert.NotNull(child2);
        Assert.Equal("Child Group 2", child2!.Title);
        Assert.Single(child2.Projects);
    }

    [Fact]
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
        Assert.NotNull(retrievedGroup);
        Assert.Equal("Updated Root Group", retrievedGroup!.Title);

        var child1 = retrievedGroup.Groups.FirstOrDefault(g => g.Id == "200");
        Assert.NotNull(child1);
        Assert.Equal("Updated Child Group 1", child1!.Title);

        Assert.True(allProjects.Count >= 6);

        var projectTitles = allProjects.ToDictionary(p => p.Id, p => p.Title);
        Assert.True(projectTitles.ContainsKey("1003"));
        Assert.Equal("New Project", projectTitles["1003"]);
    }

    [Fact]
    public async Task HasProject_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);

        // Act
        var hasProject = await service.HasProject(1001, CancellationToken.None);

        // Assert
        Assert.True(hasProject);
    }

    [Fact]
    public async Task HasProject_WithNonExistingId_ReturnsFalse()
    {
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);
        var hasProject = await service.HasProject(9999, CancellationToken.None);
        Assert.False(hasProject);
    }

    [Fact]
    public async Task EnumerateAllProjectsIds_ReturnsAllProjectIds()
    {
        var rootGroups = CreateTestHierarchy();
        await service.SaveGitLabHierarchy(rootGroups, CancellationToken.None);

        // Act
        var projectIdsEnumerable = service.EnumerateAllProjectsIds(CancellationToken.None);
        var projectIds = await projectIdsEnumerable.ToListAsync();

        // Assert
        Assert.Contains(1001, projectIds);
        Assert.Contains(1002, projectIds);
        Assert.Contains(2001, projectIds);
        Assert.Contains(2002, projectIds);
        Assert.Contains(3001, projectIds);
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

    private readonly ConnectionFactory connectionFactory = null!;
    private readonly TestCityDatabase database = null!;
    private readonly GitLabProjectsService service = null!;
}
