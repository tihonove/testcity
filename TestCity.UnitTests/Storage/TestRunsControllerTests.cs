using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TestCity.Api.Controllers;
using TestCity.Api.Models;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Storage;

[Collection("Global")]
public class TestRunsControllerTests : IAsyncLifetime, IAsyncDisposable
{
    private readonly ITestOutputHelper output;
    private readonly ConnectionFactory connectionFactory;
    private readonly TestCityDatabase database;
    private readonly GitLabProjectsService gitLabProjectsService;

    public TestRunsControllerTests(ITestOutputHelper output)
    {
        this.output = output;
        XUnitLoggerProvider.ConfigureTestLogger(output);
        
        connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        database = new TestCityDatabase(connectionFactory);
        var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        gitLabProjectsService = new GitLabProjectsService(database, clientProvider);
    }

    public async Task InitializeAsync()
    {
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    public Task DisposeAsync()
    {
        database.GitLabEntities.DeleteById(500, 600, 5001, 6001);
        gitLabProjectsService?.Dispose();
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetDashboardData_WithSingleProject_ShouldReturnProjectDashboard()
    {
        // Arrange
        var projectId = "5001";
        var groupId = "500";

        var group = new GitLabGroup
        {
            Id = groupId,
            Title = "Test Group",
            AvatarUrl = "http://test.com/avatar.png",
            Projects = new List<GitLabProject>
            {
                new()
                {
                    Id = projectId,
                    Title = "Test Project",
                    AvatarUrl = "http://test.com/project-avatar.png",
                    UseHooks = false
                }
            },
            Groups = new List<GitLabGroup>()
        };

        await gitLabProjectsService.SaveGitLabHierarchy(new List<GitLabGroup> { group }, CancellationToken.None);

        var jobId = "test-job-1";
        var jobRunId = Guid.NewGuid().ToString();

        // Создаем JobInfo
        var jobInfo = new FullJobInfo
        {
            JobId = jobId,
            JobRunId = jobRunId,
            ProjectId = projectId,
            PipelineId = "pipeline-1",
            BranchName = "main",
            AgentName = "agent-1",
            AgentOSName = "linux",
            JobUrl = "http://test.com/job",
            State = JobStatus.Success,
            Duration = 1000,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddSeconds(1),
            Triggered = null,
            PipelineSource = null,
            CommitSha = null,
            CommitMessage = null,
            CommitAuthor = null,
            TotalTestsCount = 10,
            SuccessTestsCount = 10,
            FailedTestsCount = 0,
            SkippedTestsCount = 0,
            ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
        };

        await database.JobInfo.InsertAsync(jobInfo);

        var controller = new TestRunsContoller(gitLabProjectsService, database, GitLabSettings.Default);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData()
        };
        controller.RouteData.Values["groupPath1"] = groupId;
        controller.RouteData.Values["groupPath2"] = projectId;

        // Act
        var result = await controller.GetDashboardData(branchName: "main");

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboardNode = okResult.Value.Should().BeOfType<ProjectDashboardNodeDto>().Subject;

        dashboardNode.Id.Should().Be(projectId);
        dashboardNode.Title.Should().Be("Test Project");
        dashboardNode.Type.Should().Be("project");
        dashboardNode.Jobs.Should().HaveCount(1);
        dashboardNode.Jobs[0].JobId.Should().Be(jobId);
        dashboardNode.Jobs[0].Runs.Should().HaveCount(1);
        dashboardNode.Jobs[0].Runs[0].JobId.Should().Be(jobId);
        dashboardNode.Jobs[0].Runs[0].JobRunId.Should().Be(jobRunId);
    }

    [Fact]
    public async Task GetDashboardData_WithGroup_ShouldReturnGroupDashboard()
    {
        // Arrange
        var groupId = "500";
        var childGroupId = "600";
        var projectId = "6001";

        var group = new GitLabGroup
        {
            Id = groupId,
            Title = "Parent Group",
            AvatarUrl = "http://test.com/group-avatar.png",
            Projects = new List<GitLabProject>
            {
                new()
                {
                    Id = "5001",
                    Title = "Root Project",
                    AvatarUrl = "http://test.com/project-avatar.png",
                    UseHooks = false
                }
            },
            Groups = new List<GitLabGroup>
            {
                new()
                {
                    Id = childGroupId,
                    Title = "Child Group",
                    AvatarUrl = "http://test.com/child-avatar.png",
                    Projects = new List<GitLabProject>
                    {
                        new()
                        {
                            Id = projectId,
                            Title = "Child Project",
                            AvatarUrl = null,
                            UseHooks = false
                        }
                    },
                    Groups = new List<GitLabGroup>()
                }
            }
        };

        await gitLabProjectsService.SaveGitLabHierarchy(new List<GitLabGroup> { group }, CancellationToken.None);

        var controller = new TestRunsContoller(gitLabProjectsService, database, GitLabSettings.Default);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData()
        };
        controller.RouteData.Values["groupPath1"] = groupId;

        // Act
        var result = await controller.GetDashboardData();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboardNode = okResult.Value.Should().BeOfType<GroupDashboardNodeDto>().Subject;

        dashboardNode.Id.Should().Be(groupId);
        dashboardNode.Title.Should().Be("Parent Group");
        dashboardNode.Type.Should().Be("group");
        dashboardNode.Children.Should().HaveCount(2); // 1 project + 1 child group
        dashboardNode.Children.Should().Contain(c => c.Id == "5001" && c.Type == "project");
        dashboardNode.Children.Should().Contain(c => c.Id == childGroupId && c.Type == "group");
    }
}
