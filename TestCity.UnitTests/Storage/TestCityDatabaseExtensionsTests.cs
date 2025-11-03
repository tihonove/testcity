using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Storage;

[Collection("Global")]
public class TestCityDatabaseExtensionsTests : IAsyncLifetime
{
    private readonly ITestOutputHelper output;

    public TestCityDatabaseExtensionsTests(ITestOutputHelper output)
    {
        this.output = output;
        XUnitLoggerProvider.ConfigureTestLogger(output);
    }

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
    public async Task GetFailedTestOutput_WithFailedTest_ShouldReturnCorrectOutput()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        
        var jobRunInfo = new JobRunInfo
        {
            JobId = "test-job",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = "test-project",
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "linux",
            JobUrl = "http://test.com",
            PipelineId = "pipeline-1"
        };

        var testId = $"test-{Guid.NewGuid()}";
        var failedTest = new TestRun
        {
            TestId = testId,
            TestResult = TestResult.Failed,
            Duration = 10000L, // 10 seconds in milliseconds
            StartDateTime = DateTime.UtcNow,
            JUnitFailureMessage = "Test assertion failed",
            JUnitFailureOutput = "Expected: 5, Actual: 3",
            JUnitSystemOutput = "System output for failed test"
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, new[] { failedTest });

        // Act
        var result = await database.GetFailedTestOutput(
            jobRunInfo.JobId, 
            testId, 
            new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result!.FailureMessage.Should().Be("Test assertion failed");
        result.FailureOutput.Should().Be("Expected: 5, Actual: 3");
        result.SystemOutput.Should().Be("System output for failed test");
    }

    [Fact]
    public async Task GetFailedTestOutput_WithSuccessfulTest_ShouldReturnNull()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        
        var jobRunInfo = new JobRunInfo
        {
            JobId = "test-job-success",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = "test-project",
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "linux",
            JobUrl = "http://test.com",
            PipelineId = "pipeline-1"
        };

        var testId = $"test-success-{Guid.NewGuid()}";
        var successfulTest = new TestRun
        {
            TestId = testId,
            TestResult = TestResult.Success,
            Duration = 5000L, // 5 seconds in milliseconds
            StartDateTime = DateTime.UtcNow,
            JUnitFailureMessage = null,
            JUnitFailureOutput = null,
            JUnitSystemOutput = "System output for successful test"
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, new[] { successfulTest });

        // Act
        var result = await database.GetFailedTestOutput(
            jobRunInfo.JobId, 
            testId, 
            new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFailedTestOutput_WithNonExistentTest_ShouldReturnNull()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.GetFailedTestOutput(
            "non-existent-job", 
            "non-existent-test", 
            new[] { "non-existent-jobrun" });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFailedTestOutput_WithMultipleJobRunIds_ShouldReturnMostRecentFailure()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        
        var olderJobRunInfo = new JobRunInfo
        {
            JobId = "test-job-multiple",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = "test-project",
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "linux",
            JobUrl = "http://test.com",
            PipelineId = "pipeline-1"
        };

        var newerJobRunInfo = new JobRunInfo
        {
            JobId = "test-job-multiple",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = "test-project",
            BranchName = "main", 
            AgentName = "test-agent",
            AgentOSName = "linux",
            JobUrl = "http://test.com",
            PipelineId = "pipeline-1"
        };

        var testId = $"test-multiple-{Guid.NewGuid()}";
        var olderFailedTest = new TestRun
        {
            TestId = testId,
            TestResult = TestResult.Failed,
            Duration = 10000L, // 10 seconds in milliseconds
            StartDateTime = DateTime.UtcNow.AddHours(-1),
            JUnitFailureMessage = "Older failure message",
            JUnitFailureOutput = "Older failure output",
            JUnitSystemOutput = "Older system output"
        };

        var newerFailedTest = new TestRun
        {
            TestId = testId,
            TestResult = TestResult.Failed,
            Duration = 15000L, // 15 seconds in milliseconds
            StartDateTime = DateTime.UtcNow,
            JUnitFailureMessage = "Newer failure message",
            JUnitFailureOutput = "Newer failure output",
            JUnitSystemOutput = "Newer system output"
        };

        await database.TestRuns.InsertBatchAsync(olderJobRunInfo, new[] { olderFailedTest });
        await database.TestRuns.InsertBatchAsync(newerJobRunInfo, new[] { newerFailedTest });

        // Act
        var result = await database.GetFailedTestOutput(
            "test-job-multiple", 
            testId, 
            new[] { olderJobRunInfo.JobRunId, newerJobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result!.FailureMessage.Should().Be("Newer failure message");
        result.FailureOutput.Should().Be("Newer failure output");
        result.SystemOutput.Should().Be("Newer system output");
    }

    [Fact]
    public async Task GetFailedTestOutput_WithNullFailureMessages_ShouldReturnEmptyStrings()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        
        var jobRunInfo = new JobRunInfo
        {
            JobId = "test-job-null",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = "test-project",
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "linux",
            JobUrl = "http://test.com",
            PipelineId = "pipeline-1"
        };

        var testId = $"test-null-{Guid.NewGuid()}";
        var failedTestWithNulls = new TestRun
        {
            TestId = testId,
            TestResult = TestResult.Failed,
            Duration = 10000L, // 10 seconds in milliseconds
            StartDateTime = DateTime.UtcNow,
            JUnitFailureMessage = null,
            JUnitFailureOutput = null,
            JUnitSystemOutput = null
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, new[] { failedTestWithNulls });

        // Act
        var result = await database.GetFailedTestOutput(
            jobRunInfo.JobId, 
            testId, 
            new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result!.FailureMessage.Should().Be(string.Empty);
        result.FailureOutput.Should().Be(string.Empty);
        result.SystemOutput.Should().Be(string.Empty);
    }

    [Fact]
    public async Task FindAllJobs_WithMultipleProjects_ShouldReturnDistinctJobs()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var project1Id = $"project-{Guid.NewGuid()}";
        var project2Id = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project1Id,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project2Id,
                PipelineId = "pipeline-2",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 2000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobs(new[] { project1Id, project2Id });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(j => j.JobId == "job-1" && j.ProjectId == project1Id);
        result.Should().Contain(j => j.JobId == "job-2" && j.ProjectId == project2Id);
    }

    [Fact]
    public async Task FindAllJobs_WithOldJobs_ShouldExcludeJobsOlderThan14Days()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "recent-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/recent",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-7),
                EndDateTime = DateTime.UtcNow.AddDays(-7).AddSeconds(1),
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
            },
            new()
            {
                JobId = "old-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-20),
                EndDateTime = DateTime.UtcNow.AddDays(-20).AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobs(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain(j => j.JobId == "recent-job" && j.ProjectId == projectId);
        result.Should().NotContain(j => j.JobId == "old-job");
    }

    [Fact]
    public async Task FindAllJobs_WithDuplicateJobRuns_ShouldReturnDistinctJobs()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "same-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/run1",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-1),
                EndDateTime = DateTime.UtcNow.AddDays(-1).AddSeconds(1),
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
            },
            new()
            {
                JobId = "same-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/run2",
                State = JobStatus.Success,
                Duration = 1500,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1.5),
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
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobs(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].JobId.Should().Be("same-job");
        result[0].ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task FindAllJobs_WithEmptyProjectIds_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.FindAllJobs(Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAllJobs_WithNonExistentProjectId_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.FindAllJobs(new[] { "non-existent-project" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAllJobsRunsInProgress_WithInProgressJobs_ShouldReturnCorrectData()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var project1Id = $"project-{Guid.NewGuid()}";
        var project2Id = $"project-{Guid.NewGuid()}";

        var inProgressJobs = new List<InProgressJobInfo>
        {
            new()
            {
                JobId = "in-progress-job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project1Id,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/inprogress1",
                StartDateTime = DateTime.UtcNow.AddMinutes(-5),
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "in-progress-job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project2Id,
                PipelineId = "pipeline-2",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "windows",
                JobUrl = "http://test.com/inprogress2",
                StartDateTime = DateTime.UtcNow.AddMinutes(-10),
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        foreach (var job in inProgressJobs)
        {
            await database.InProgressJobInfo.InsertAsync(job);
        }

        // Act
        var result = await database.FindAllJobsRunsInProgress(new[] { project1Id, project2Id });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        var job1 = result.FirstOrDefault(j => j.JobId == "in-progress-job-1");
        job1.Should().NotBeNull();
        job1!.State.Should().Be("Running");
        job1.ProjectId.Should().Be(project1Id);
        job1.BranchName.Should().Be("main");
        job1.AgentName.Should().Be("agent-1");
        job1.TotalTestsCount.Should().BeNull();
        job1.Duration.Should().BeNull();

        var job2 = result.FirstOrDefault(j => j.JobId == "in-progress-job-2");
        job2.Should().NotBeNull();
        job2!.State.Should().Be("Running");
        job2.ProjectId.Should().Be(project2Id);
    }

    [Fact]
    public async Task FindAllJobsRunsInProgress_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var inProgressJobs = new List<InProgressJobInfo>
        {
            new()
            {
                JobId = "job-main",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/main",
                StartDateTime = DateTime.UtcNow,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "job-develop",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/develop",
                StartDateTime = DateTime.UtcNow,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        foreach (var job in inProgressJobs)
        {
            await database.InProgressJobInfo.InsertAsync(job);
        }

        // Act
        var result = await database.FindAllJobsRunsInProgress(new[] { projectId }, "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].JobId.Should().Be("job-main");
        result[0].BranchName.Should().Be("main");
    }

    [Fact]
    public async Task FindAllJobsRunsInProgress_ShouldExcludeCompletedJobs()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var sharedJobRunId = Guid.NewGuid().ToString();

        // Создаем in-progress job
        var inProgressJob = new InProgressJobInfo
        {
            JobId = "job-1",
            JobRunId = sharedJobRunId,
            ProjectId = projectId,
            PipelineId = "pipeline-1",
            BranchName = "main",
            AgentName = "agent-1",
            AgentOSName = "linux",
            JobUrl = "http://test.com/job",
            StartDateTime = DateTime.UtcNow,
            ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
        };

        await database.InProgressJobInfo.InsertAsync(inProgressJob);

        // Создаем completed job с тем же JobRunId
        var completedJob = new FullJobInfo
        {
            JobId = "job-1",
            JobRunId = sharedJobRunId,
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

        await database.JobInfo.InsertAsync(completedJob);

        // Act
        var result = await database.FindAllJobsRunsInProgress(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("completed job should be excluded from in-progress jobs");
    }

    [Fact]
    public async Task FindAllJobsRunsInProgress_WithOldJobs_ShouldExcludeJobsOlderThan14Days()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var inProgressJobs = new List<InProgressJobInfo>
        {
            new()
            {
                JobId = "recent-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/recent",
                StartDateTime = DateTime.UtcNow.AddDays(-7),
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "old-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old",
                StartDateTime = DateTime.UtcNow.AddDays(-20),
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        foreach (var job in inProgressJobs)
        {
            await database.InProgressJobInfo.InsertAsync(job);
        }

        // Act
        var result = await database.FindAllJobsRunsInProgress(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].JobId.Should().Be("recent-job");
    }

    [Fact]
    public async Task FindAllJobsRunsInProgress_WithEmptyProjectIds_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.FindAllJobsRunsInProgress(Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAllJobsRuns_WithMultipleProjects_ShouldReturnLatestRunsPerBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "job-1";

        var jobs = new List<FullJobInfo>
        {
            // Older run on main branch
            new()
            {
                JobId = jobId,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/older",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-2),
                EndDateTime = DateTime.UtcNow.AddDays(-2).AddSeconds(1),
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
            },
            // Newer run on main branch
            new()
            {
                JobId = jobId,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/newer",
                State = JobStatus.Success,
                Duration = 1200,
                StartDateTime = DateTime.UtcNow.AddDays(-1),
                EndDateTime = DateTime.UtcNow.AddDays(-1).AddSeconds(1.2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 12,
                SuccessTestsCount = 12,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            // Run on develop branch
            new()
            {
                JobId = jobId,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-3",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/develop",
                State = JobStatus.Failed,
                Duration = 900,
                StartDateTime = DateTime.UtcNow.AddHours(-6),
                EndDateTime = DateTime.UtcNow.AddHours(-6).AddSeconds(0.9),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 8,
                SuccessTestsCount = 7,
                FailedTestsCount = 1,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        
        // Should return latest run per branch
        var mainRuns = result.Where(r => r.BranchName == "main").ToList();
        mainRuns.Should().HaveCount(1);
        mainRuns[0].StartDateTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(10));

        var developRuns = result.Where(r => r.BranchName == "develop").ToList();
        developRuns.Should().HaveCount(1);
        developRuns[0].State.Should().Be("Failed");
    }

    [Fact]
    public async Task FindAllJobsRuns_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-main",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/main",
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
            },
            new()
            {
                JobId = "job-develop",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/develop",
                State = JobStatus.Success,
                Duration = 1500,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1.5),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId }, "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].JobId.Should().Be("job-main");
        result[0].BranchName.Should().Be("main");
    }

    [Fact]
    public async Task FindAllJobsRuns_ShouldLimitTo5RunsPerJob()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "job-with-many-runs";

        // Create 7 runs for the same job on different branches
        var jobs = new List<FullJobInfo>();
        for (int i = 0; i < 7; i++)
        {
            jobs.Add(new FullJobInfo
            {
                JobId = jobId,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = $"pipeline-{i}",
                BranchName = $"branch-{i}",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = $"http://test.com/run{i}",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-i - 5), // Older than 3 days
                EndDateTime = DateTime.UtcNow.AddDays(-i - 5).AddSeconds(1),
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
            });
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        // Should return max 5 runs per job (rnj <= 5), unless they're within 3 days
        result.Where(r => r.JobId == jobId).Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public async Task FindAllJobsRuns_ShouldIncludeRecentJobsRegardlessOfLimit()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "job-with-recent-runs";

        // Create 7 runs within last 2 days (should all be included)
        var jobs = new List<FullJobInfo>();
        for (int i = 0; i < 7; i++)
        {
            jobs.Add(new FullJobInfo
            {
                JobId = jobId,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = $"pipeline-{i}",
                BranchName = $"branch-{i}",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = $"http://test.com/run{i}",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddHours(-i * 6), // Within last 2 days
                EndDateTime = DateTime.UtcNow.AddHours(-i * 6).AddSeconds(1),
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
            });
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        // All 7 runs should be included because they're within 3 days
        result.Where(r => r.JobId == jobId).Should().HaveCount(7);
    }

    [Fact]
    public async Task FindAllJobsRuns_WithOldJobs_ShouldExcludeJobsOlderThan14Days()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "recent-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/recent",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-7),
                EndDateTime = DateTime.UtcNow.AddDays(-7).AddSeconds(1),
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
            },
            new()
            {
                JobId = "old-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-20),
                EndDateTime = DateTime.UtcNow.AddDays(-20).AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(j => j.JobId == "recent-job");
        result.Should().NotContain(j => j.JobId == "old-job");
    }

    [Fact]
    public async Task FindAllJobsRuns_WithEmptyProjectIds_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.FindAllJobsRuns(Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAllJobsRuns_ShouldReturnCorrectCommitCount()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var changesSinceLastRun = new List<CommitParentsChangesEntry>
        {
            new() { ParentCommitSha = "commit1", Depth = 0, AuthorName = "Author1", AuthorEmail = "author1@test.com", MessagePreview = "Message 1" },
            new() { ParentCommitSha = "commit2", Depth = 1, AuthorName = "Author2", AuthorEmail = "author2@test.com", MessagePreview = "Message 2" },
            new() { ParentCommitSha = "commit3", Depth = 2, AuthorName = "Author3", AuthorEmail = "author3@test.com", MessagePreview = "Message 3" }
        };

        var job = new FullJobInfo
        {
            JobId = "job-with-commits",
            JobRunId = Guid.NewGuid().ToString(),
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
            CommitSha = "commit1",
            CommitMessage = "Latest commit",
            CommitAuthor = "Author1",
            TotalTestsCount = 10,
            SuccessTestsCount = 10,
            FailedTestsCount = 0,
            SkippedTestsCount = 0,
            ChangesSinceLastRun = changesSinceLastRun
        };

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(3);
        result[0].ChangesSinceLastRun.Should().HaveCountLessOrEqualTo(20); // arraySlice(, 1, 20)
    }

    [Fact]
    public async Task FindBranches_WithMultipleProjects_ShouldReturnDistinctBranches()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var project1Id = $"project-{Guid.NewGuid()}";
        var project2Id = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project1Id,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project2Id,
                PipelineId = "pipeline-2",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 2000,
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                EndDateTime = DateTime.UtcNow.AddHours(-1).AddSeconds(2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "job-3",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project1Id,
                PipelineId = "pipeline-3",
                BranchName = "feature/test",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job3",
                State = JobStatus.Success,
                Duration = 1500,
                StartDateTime = DateTime.UtcNow.AddHours(-2),
                EndDateTime = DateTime.UtcNow.AddHours(-2).AddSeconds(1.5),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 8,
                SuccessTestsCount = 8,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindBranches(new[] { project1Id, project2Id });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(3);
        result.Should().Contain("main");
        result.Should().Contain("develop");
        result.Should().Contain("feature/test");
    }

    [Fact]
    public async Task FindBranches_WithJobIdFilter_ShouldReturnBranchesForSpecificJob()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var job1Id = "specific-job";
        var job2Id = "other-job";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = job1Id,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = job1Id,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "develop",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1-2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                EndDateTime = DateTime.UtcNow.AddHours(-1).AddSeconds(1),
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
            },
            new()
            {
                JobId = job2Id,
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-3",
                BranchName = "feature/other",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 2000,
                StartDateTime = DateTime.UtcNow.AddHours(-2),
                EndDateTime = DateTime.UtcNow.AddHours(-2).AddSeconds(2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindBranches(jobId: job1Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain("main");
        result.Should().Contain("develop");
        result.Should().NotContain("feature/other");
    }

    [Fact]
    public async Task FindBranches_ShouldExcludeEmptyBranchNames()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-with-branch",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = "job-without-branch",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                EndDateTime = DateTime.UtcNow.AddHours(-1).AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindBranches(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("main");
        result.Should().NotContain("");
    }

    [Fact]
    public async Task FindBranches_ShouldExcludeJobsOlderThan14Days()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "recent-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/recent",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-7),
                EndDateTime = DateTime.UtcNow.AddDays(-7).AddSeconds(1),
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
            },
            new()
            {
                JobId = "old-job",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "old-branch",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-20),
                EndDateTime = DateTime.UtcNow.AddDays(-20).AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindBranches(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("main");
        result.Should().NotContain("old-branch");
    }

    [Fact]
    public async Task FindBranches_WithNoFilters_ShouldReturnAllBranches()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var job = new FullJobInfo
        {
            JobId = "test-job",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            PipelineId = "pipeline-1",
            BranchName = "test-branch",
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

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.FindBranches();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("test-branch");
    }

    [Fact]
    public async Task FindBranches_ShouldReturnDistinctBranches()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                EndDateTime = DateTime.UtcNow.AddHours(-1).AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindBranches(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        // Should return "main" only once even though there are 2 jobs on this branch
        result.Where(b => b == "main").Should().HaveCount(1);
    }

    [Fact]
    public async Task FindBranches_ShouldOrderByStartDateTimeDesc()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "older-branch",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-5),
                EndDateTime = DateTime.UtcNow.AddDays(-5).AddSeconds(1),
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
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-2",
                BranchName = "newer-branch",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindBranches(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        // Newer branch should come first due to ORDER BY StartDateTime DESC
        var newerBranchIndex = Array.IndexOf(result, "newer-branch");
        var olderBranchIndex = Array.IndexOf(result, "older-branch");
        newerBranchIndex.Should().BeLessThan(olderBranchIndex);
    }

    [Fact]
    public async Task GetPipelineRunsByProject_WithMultipleJobsInPipeline_ShouldAggregateCorrectly()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var pipelineId = $"pipeline-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = "abc123",
                CommitMessage = "Test commit",
                CommitAuthor = "Author",
                TotalTestsCount = 10,
                SuccessTestsCount = 10,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 2000,
                StartDateTime = DateTime.UtcNow.AddMinutes(-1),
                EndDateTime = DateTime.UtcNow.AddMinutes(-1).AddSeconds(2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = "abc123",
                CommitMessage = "Test commit",
                CommitAuthor = "Author",
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var pipelineRun = result[0];
        pipelineRun.ProjectId.Should().Be(projectId);
        pipelineRun.PipelineId.Should().Be(pipelineId);
        pipelineRun.BranchName.Should().Be("main");
        pipelineRun.TotalTestsCount.Should().Be(15); // 10 + 5
        pipelineRun.Duration.Should().Be(3000); // 1000 + 2000
        pipelineRun.SuccessTestsCount.Should().Be(15); // 10 + 5
        pipelineRun.FailedTestsCount.Should().Be(0);
        pipelineRun.SkippedTestsCount.Should().Be(0);
        pipelineRun.JobRunCount.Should().Be(2);
        pipelineRun.CommitSha.Should().Be("abc123");
        pipelineRun.CommitMessage.Should().Be("Test commit");
        pipelineRun.CommitAuthor.Should().Be("Author");
    }

    [Fact]
    public async Task GetPipelineRunsByProject_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-main",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-main",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/main",
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
            },
            new()
            {
                JobId = "job-develop",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-develop",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/develop",
                State = JobStatus.Success,
                Duration = 1500,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1.5),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId, "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PipelineId.Should().Be("pipeline-main");
        result[0].BranchName.Should().Be("main");
    }

    [Fact]
    public async Task GetPipelineRunsByProject_ShouldOnlyReturnPipelinesWithNonEmptyPipelineId()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-with-pipeline",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = "job-without-pipeline",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PipelineId.Should().Be("pipeline-1");
    }

    [Fact]
    public async Task GetPipelineRunsByProject_ShouldAggregateMaxStateCorrectly()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var pipelineId = $"pipeline-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-success",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/success",
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
            },
            new()
            {
                JobId = "job-failed",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/failed",
                State = JobStatus.Failed,
                Duration = 500,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(0.5),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 4,
                FailedTestsCount = 1,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].State.Should().Be("Failed"); // MAX(State) should prefer Failed over Success
    }

    [Fact]
    public async Task GetPipelineRunsByProject_ShouldConcatenateCustomStatusMessages()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var pipelineId = $"pipeline-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                CustomStatusMessage = "Message 1",
                TotalTestsCount = 10,
                SuccessTestsCount = 10,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                CustomStatusMessage = "Message 2",
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].CustomStatusMessage.Should().Contain("Message 1");
        result[0].CustomStatusMessage.Should().Contain("Message 2");
    }

    [Fact]
    public async Task GetPipelineRunsByProject_ShouldOrderByStartDateTimeDesc()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-old",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-old",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-5),
                EndDateTime = DateTime.UtcNow.AddDays(-5).AddSeconds(1),
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
            },
            new()
            {
                JobId = "job-new",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-new",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/new",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].PipelineId.Should().Be("pipeline-new");
        result[1].PipelineId.Should().Be("pipeline-old");
    }

    [Fact]
    public async Task GetPipelineRunsByProject_ShouldLimitTo200Results()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>();
        for (int i = 0; i < 250; i++)
        {
            jobs.Add(new FullJobInfo
            {
                JobId = $"job-{i}",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = $"pipeline-{i}",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = $"http://test.com/job{i}",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddMinutes(-i),
                EndDateTime = DateTime.UtcNow.AddMinutes(-i).AddSeconds(1),
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
            });
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(200);
    }

    [Fact]
    public async Task GetPipelineRunsByProject_WithNonExistentProject_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.GetPipelineRunsByProject("non-existent-project");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPipelineRunsByProject_ShouldHandleChangesSinceLastRun()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var pipelineId = $"pipeline-{Guid.NewGuid()}";

        var changesSinceLastRun = new List<CommitParentsChangesEntry>
        {
            new() { ParentCommitSha = "commit1", Depth = 0, AuthorName = "Author1", AuthorEmail = "author1@test.com", MessagePreview = "Message 1" },
            new() { ParentCommitSha = "commit2", Depth = 1, AuthorName = "Author2", AuthorEmail = "author2@test.com", MessagePreview = "Message 2" }
        };

        var job = new FullJobInfo
        {
            JobId = "job-with-commits",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            PipelineId = pipelineId,
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
            CommitSha = "commit1",
            CommitMessage = "Latest commit",
            CommitAuthor = "Author1",
            TotalTestsCount = 10,
            SuccessTestsCount = 10,
            FailedTestsCount = 0,
            SkippedTestsCount = 0,
            ChangesSinceLastRun = changesSinceLastRun
        };

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(2);
        result[0].ChangesSinceLastRun.Should().HaveCountLessOrEqualTo(20);
    }

    [Fact]
    public async Task GetPipelineRunsOverview_WithMultipleProjects_ShouldReturnLatestRunPerBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var project1Id = $"project-{Guid.NewGuid()}";
        var project2Id = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            // Project 1 - main branch - older run
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project1Id,
                PipelineId = "pipeline-1-old",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1-old",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddHours(-2),
                EndDateTime = DateTime.UtcNow.AddHours(-2).AddSeconds(1),
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
            },
            // Project 1 - main branch - newer run (should be returned)
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project1Id,
                PipelineId = "pipeline-1-new",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1-new",
                State = JobStatus.Success,
                Duration = 1200,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1.2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 12,
                SuccessTestsCount = 12,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            // Project 2 - develop branch
            new()
            {
                JobId = "job-3",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = project2Id,
                PipelineId = "pipeline-2",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job3",
                State = JobStatus.Failed,
                Duration = 900,
                StartDateTime = DateTime.UtcNow.AddMinutes(-30),
                EndDateTime = DateTime.UtcNow.AddMinutes(-30).AddSeconds(0.9),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 8,
                SuccessTestsCount = 7,
                FailedTestsCount = 1,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { project1Id, project2Id });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2, "should return one latest run per project-branch combination");
        
        var project1Run = result.FirstOrDefault(r => r.ProjectId == project1Id);
        project1Run.Should().NotBeNull();
        project1Run!.PipelineId.Should().Be("pipeline-1-new", "should return the latest run for main branch");
        project1Run.BranchName.Should().Be("main");

        var project2Run = result.FirstOrDefault(r => r.ProjectId == project2Id);
        project2Run.Should().NotBeNull();
        project2Run!.PipelineId.Should().Be("pipeline-2");
        project2Run.BranchName.Should().Be("develop");
    }

    [Fact]
    public async Task GetPipelineRunsOverview_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-main",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-main",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/main",
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
            },
            new()
            {
                JobId = "job-develop",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-develop",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/develop",
                State = JobStatus.Success,
                Duration = 1500,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1.5),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId }, "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PipelineId.Should().Be("pipeline-main");
        result[0].BranchName.Should().Be("main");
    }

    [Fact]
    public async Task GetPipelineRunsOverview_ShouldOnlyReturnPipelinesWithNonEmptyPipelineId()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-with-pipeline",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-1",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
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
            },
            new()
            {
                JobId = "job-without-pipeline",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "",
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PipelineId.Should().Be("pipeline-1");
    }

    [Fact]
    public async Task GetPipelineRunsOverview_ShouldReturnOnlyLatestRunPerBranch()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            // Older pipeline on main branch
            new()
            {
                JobId = "job-old-main",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-old-main",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old-main",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-1),
                EndDateTime = DateTime.UtcNow.AddDays(-1).AddSeconds(1),
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
            },
            // Newer pipeline on main branch
            new()
            {
                JobId = "job-new-main",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-new-main",
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/new-main",
                State = JobStatus.Success,
                Duration = 1200,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1.2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 12,
                SuccessTestsCount = 12,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            // Pipeline on develop branch
            new()
            {
                JobId = "job-develop",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-develop",
                BranchName = "develop",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/develop",
                State = JobStatus.Success,
                Duration = 900,
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                EndDateTime = DateTime.UtcNow.AddHours(-1).AddSeconds(0.9),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 8,
                SuccessTestsCount = 8,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2, "should return only latest run per branch");
        
        var mainRun = result.FirstOrDefault(r => r.BranchName == "main");
        mainRun.Should().NotBeNull();
        mainRun!.PipelineId.Should().Be("pipeline-new-main", "should return the newest pipeline on main branch");

        var developRun = result.FirstOrDefault(r => r.BranchName == "develop");
        developRun.Should().NotBeNull();
        developRun!.PipelineId.Should().Be("pipeline-develop");
    }

    [Fact]
    public async Task GetPipelineRunsOverview_ShouldAggregateJobsInSamePipeline()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var pipelineId = $"pipeline-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-1",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job1",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = "abc123",
                CommitMessage = "Test commit",
                CommitAuthor = "Author",
                TotalTestsCount = 10,
                SuccessTestsCount = 10,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            },
            new()
            {
                JobId = "job-2",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = pipelineId,
                BranchName = "main",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/job2",
                State = JobStatus.Success,
                Duration = 2000,
                StartDateTime = DateTime.UtcNow.AddMinutes(-1),
                EndDateTime = DateTime.UtcNow.AddMinutes(-1).AddSeconds(2),
                Triggered = null,
                PipelineSource = null,
                CommitSha = "abc123",
                CommitMessage = "Test commit",
                CommitAuthor = "Author",
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var pipelineRun = result[0];
        pipelineRun.ProjectId.Should().Be(projectId);
        pipelineRun.PipelineId.Should().Be(pipelineId);
        pipelineRun.BranchName.Should().Be("main");
        pipelineRun.TotalTestsCount.Should().Be(15); // 10 + 5
        pipelineRun.Duration.Should().Be(3000); // 1000 + 2000
        pipelineRun.SuccessTestsCount.Should().Be(15); // 10 + 5
        pipelineRun.JobRunCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPipelineRunsOverview_ShouldLimitTo1000Results()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>();
        // Create 1100 pipelines (each on different branch to avoid rn filtering)
        for (int i = 0; i < 1100; i++)
        {
            jobs.Add(new FullJobInfo
            {
                JobId = $"job-{i}",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = $"pipeline-{i}",
                BranchName = $"branch-{i}",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = $"http://test.com/job{i}",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddMinutes(-i),
                EndDateTime = DateTime.UtcNow.AddMinutes(-i).AddSeconds(1),
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
            });
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(1000);
    }

    [Fact]
    public async Task GetPipelineRunsOverview_WithEmptyProjectIds_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.GetPipelineRunsOverview(Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPipelineRunsOverview_WithNonExistentProject_ShouldReturnEmptyArray()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { "non-existent-project" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPipelineRunsOverview_ShouldOrderByStartDateTimeDesc()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            new()
            {
                JobId = "job-old",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-old",
                BranchName = "branch-old",
                AgentName = "agent-1",
                AgentOSName = "linux",
                JobUrl = "http://test.com/old",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow.AddDays(-5),
                EndDateTime = DateTime.UtcNow.AddDays(-5).AddSeconds(1),
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
            },
            new()
            {
                JobId = "job-new",
                JobRunId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                PipelineId = "pipeline-new",
                BranchName = "branch-new",
                AgentName = "agent-2",
                AgentOSName = "linux",
                JobUrl = "http://test.com/new",
                State = JobStatus.Success,
                Duration = 1000,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddSeconds(1),
                Triggered = null,
                PipelineSource = null,
                CommitSha = null,
                CommitMessage = null,
                CommitAuthor = null,
                TotalTestsCount = 5,
                SuccessTestsCount = 5,
                FailedTestsCount = 0,
                SkippedTestsCount = 0,
                ChangesSinceLastRun = new List<CommitParentsChangesEntry>()
            }
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].PipelineId.Should().Be("pipeline-new", "newer pipeline should come first");
        result[1].PipelineId.Should().Be("pipeline-old");
    }

    [Fact]
    public async Task GetPipelineRunsOverview_ShouldHandleChangesSinceLastRun()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";
        var pipelineId = $"pipeline-{Guid.NewGuid()}";

        var changesSinceLastRun = new List<CommitParentsChangesEntry>
        {
            new() { ParentCommitSha = "commit1", Depth = 0, AuthorName = "Author1", AuthorEmail = "author1@test.com", MessagePreview = "Message 1" },
            new() { ParentCommitSha = "commit2", Depth = 1, AuthorName = "Author2", AuthorEmail = "author2@test.com", MessagePreview = "Message 2" },
            new() { ParentCommitSha = "commit3", Depth = 2, AuthorName = "Author3", AuthorEmail = "author3@test.com", MessagePreview = "Message 3" }
        };

        var job = new FullJobInfo
        {
            JobId = "job-with-commits",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            PipelineId = pipelineId,
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
            CommitSha = "commit1",
            CommitMessage = "Latest commit",
            CommitAuthor = "Author1",
            TotalTestsCount = 10,
            SuccessTestsCount = 10,
            FailedTestsCount = 0,
            SkippedTestsCount = 0,
            ChangesSinceLastRun = changesSinceLastRun
        };

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(3);
        result[0].ChangesSinceLastRun.Should().HaveCountLessOrEqualTo(20);
    }
}

