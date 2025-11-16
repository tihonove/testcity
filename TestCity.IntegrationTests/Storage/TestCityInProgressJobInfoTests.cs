using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using Xunit;

namespace TestCity.IntegrationTests.Storage;

[Collection("Global")]
public class TestCityInProgressJobInfoTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Insert_ShouldCorrectlyAddRecord()
    {
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        var job = CreateTestInProgressJobInfo();
        await inProgressJobInfo.InsertAsync(job);

        var exists = await inProgressJobInfo.ExistsAsync(job.ProjectId, job.JobRunId);
        exists.Should().BeTrue("запись должна существовать после вставки");
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenRecordDoesNotExist()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        const string projectId = "non-existent-project-id";
        const string jobRunId = "non-existent-job-run-id";

        // Act & Assert
        var exists = await inProgressJobInfo.ExistsAsync(projectId, jobRunId);
        exists.Should().BeFalse("запись не должна существовать");
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenRecordExists()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        var job = CreateTestInProgressJobInfo();
        // Act & Assert
        await inProgressJobInfo.InsertAsync(job);
        
        var exists = await inProgressJobInfo.ExistsAsync(job.ProjectId, job.JobRunId);
        exists.Should().BeTrue("запись должна существовать после вставки");
    }

    [Fact]
    public async Task ExistsAsync_WithProjectId_ShouldReturnFalse_WhenRecordDoesNotExist()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        const string projectId = "non-existent-project-id";
        const string jobRunId = "non-existent-job-run-id";

        // Act & Assert
        var exists = await inProgressJobInfo.ExistsAsync(projectId, jobRunId);
        exists.Should().BeFalse("запись не должна существовать");
    }

    [Fact]
    public async Task ExistsAsync_WithProjectId_ShouldReturnTrue_WhenRecordExists()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        var job = CreateTestInProgressJobInfo();
        // Act & Assert
        await inProgressJobInfo.InsertAsync(job);
        
        var exists = await inProgressJobInfo.ExistsAsync(job.ProjectId, job.JobRunId);
        exists.Should().BeTrue("запись должна существовать при поиске по ProjectId после вставки");
    }

    private static InProgressJobInfo CreateTestInProgressJobInfo()
    {
        return new InProgressJobInfo
        {
            JobId = $"test-job-id-{Guid.NewGuid()}",
            JobRunId = $"test-job-run-id-{Guid.NewGuid()}",
            JobUrl = "https://example.com/job/123",
            StartDateTime = DateTime.UtcNow,
            PipelineSource = "test-pipeline",
            Triggered = "manual",
            BranchName = "main",
            CommitSha = "abcdef1234567890",
            CommitMessage = "Test commit message",
            CommitAuthor = "Test Author",
            AgentName = "test-agent",
            AgentOSName = "Linux",
            ProjectId = "test-project-id",
            PipelineId = "test-pipeline-id"
        };
    }
}
