using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using NUnit.Framework;

namespace TestCity.UnitTests.Storage;

public class TestCityInProgressJobInfoTests
{
    [SetUp]
    public async Task SetUp()
    {
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    [Test]
    public async Task Insert_ShouldCorrectlyAddRecord()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        var job = CreateTestInProgressJobInfo();

        // Act
        await inProgressJobInfo.InsertAsync(job);

        // Assert
        var exists = await inProgressJobInfo.ExistsAsync(job.ProjectId, job.JobRunId);
        Assert.That(exists, Is.True, "Запись должна существовать после вставки");
    }

    [Test]
    public async Task ExistsAsync_ShouldReturnFalse_WhenRecordDoesNotExist()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        const string projectId = "non-existent-project-id";
        const string jobRunId = "non-existent-job-run-id";

        // Act
        var exists = await inProgressJobInfo.ExistsAsync(projectId, jobRunId);

        // Assert
        Assert.That(exists, Is.False, "Запись не должна существовать");
    }

    [Test]
    public async Task ExistsAsync_ShouldReturnTrue_WhenRecordExists()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        var job = CreateTestInProgressJobInfo();
        // Act
        await inProgressJobInfo.InsertAsync(job);
        var exists = await inProgressJobInfo.ExistsAsync(job.ProjectId, job.JobRunId);

        // Assert
        Assert.That(exists, Is.True, "Запись должна существовать после вставки");
    }

    [Test]
    public async Task ExistsAsync_WithProjectId_ShouldReturnFalse_WhenRecordDoesNotExist()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        const string projectId = "non-existent-project-id";
        const string jobRunId = "non-existent-job-run-id";

        // Act
        var exists = await inProgressJobInfo.ExistsAsync(projectId, jobRunId);

        // Assert
        Assert.That(exists, Is.False, "Запись не должна существовать");
    }

    [Test]
    public async Task ExistsAsync_WithProjectId_ShouldReturnTrue_WhenRecordExists()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var inProgressJobInfo = new TestCityDatabase(connectionFactory).InProgressJobInfo;
        var job = CreateTestInProgressJobInfo();
        // Act
        await inProgressJobInfo.InsertAsync(job);
        var exists = await inProgressJobInfo.ExistsAsync(job.ProjectId, job.JobRunId);

        // Assert
        Assert.That(exists, Is.True, "Запись должна существовать при поиске по ProjectId после вставки");
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
