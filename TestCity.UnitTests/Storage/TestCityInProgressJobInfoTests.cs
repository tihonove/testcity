using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Storage;
using Kontur.TestCity.Core.Storage.DTO;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.Storage;

public class TestCityInProgressJobInfoTests
{
    [Test]
    public async Task Insert_ShouldCorrectlyAddRecord()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory();
        var inProgressJobInfo = new TestCityInProgressJobInfo(connectionFactory);
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
        var connectionFactory = new ConnectionFactory();
        var inProgressJobInfo = new TestCityInProgressJobInfo(connectionFactory);
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
        var connectionFactory = new ConnectionFactory();
        var inProgressJobInfo = new TestCityInProgressJobInfo(connectionFactory);
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
        var connectionFactory = new ConnectionFactory();
        var inProgressJobInfo = new TestCityInProgressJobInfo(connectionFactory);
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
        var connectionFactory = new ConnectionFactory();
        var inProgressJobInfo = new TestCityInProgressJobInfo(connectionFactory);
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
