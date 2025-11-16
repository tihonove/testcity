using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;
using static TestCity.UnitTests.Utils.TestDataBuilders;

namespace TestCity.IntegrationTests.Storage;

[Collection("Global")]
public class TestCityDatabaseExtensionsTests : IAsyncLifetime
{
    private readonly ITestOutputHelper output;
    private ConnectionFactory connectionFactory = null!;
    private TestCityDatabase database = null!;

    public TestCityDatabaseExtensionsTests(ITestOutputHelper output)
    {
        this.output = output;
        XUnitLoggerProvider.ConfigureTestLogger(output);
    }

    public async Task InitializeAsync()
    {
        connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        database = new TestCityDatabase(connectionFactory);
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetTestOutput_WithFailedTest_ShouldReturnCorrectOutput()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var testId = $"test-{Guid.NewGuid()}";
        var failedTest = CreateTestRun(
            testId: testId,
            result: TestResult.Failed,
            duration: 10000L,
            failureMessage: "Test assertion failed",
            failureOutput: "Expected: 5, Actual: 3",
            systemOutput: "System output for failed test");

        await database.TestRuns.InsertBatchAsync(jobRunInfo, new[] { failedTest });

        // Act
        var result = await database.GetTestOutput(
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
    public async Task GetTestOutput_WithSuccessfulTest_ShouldReturnNull()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo(jobId: "test-job-success");
        var testId = $"test-success-{Guid.NewGuid()}";
        var successfulTest = CreateTestRun(
            testId: testId,
            systemOutput: "System output for successful test");

        await database.TestRuns.InsertBatchAsync(jobRunInfo, new[] { successfulTest });

        // Act
        var result = await database.GetTestOutput(
            jobRunInfo.JobId,
            testId,
            new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTestOutput_WithNonExistentTest_ShouldReturnNull()
    {
        // Arrange - no setup needed

        // Act
        var result = await database.GetTestOutput(
            "non-existent-job",
            "non-existent-test",
            new[] { "non-existent-jobrun" });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTestOutput_WithMultipleJobRunIds_ShouldReturnMostRecentFailure()
    {
        // Arrange
        var olderJobRunInfo = CreateJobRunInfo(jobId: "test-job-multiple");
        var newerJobRunInfo = CreateJobRunInfo(jobId: "test-job-multiple");

        var testId = $"test-multiple-{Guid.NewGuid()}";
        var olderFailedTest = CreateTestRun(
            testId: testId,
            result: TestResult.Failed,
            duration: 10000L,
            startDateTime: DateTime.UtcNow.AddHours(-1),
            failureMessage: "Older failure message",
            failureOutput: "Older failure output",
            systemOutput: "Older system output");

        var newerFailedTest = CreateTestRun(
            testId: testId,
            result: TestResult.Failed,
            duration: 15000L,
            startDateTime: DateTime.UtcNow,
            failureMessage: "Newer failure message",
            failureOutput: "Newer failure output",
            systemOutput: "Newer system output");

        await database.TestRuns.InsertBatchAsync(olderJobRunInfo, new[] { olderFailedTest });
        await database.TestRuns.InsertBatchAsync(newerJobRunInfo, new[] { newerFailedTest });

        // Act
        var result = await database.GetTestOutput(
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
    public async Task GetTestOutput_WithNullFailureMessages_ShouldReturnEmptyStrings()
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
        var result = await database.GetTestOutput(
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
        var project1Id = $"project-{Guid.NewGuid()}";
        var project2Id = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: "job-1", projectId: project1Id),
            CreateFullJobInfo(jobId: "job-2", projectId: project2Id, pipelineId: "pipeline-2", agentName: "agent-2", duration: 2000, totalTests: 5, successTests: 5)
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
        var projectId = $"project-{Guid.NewGuid()}";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: "recent-job", projectId: projectId, startDateTime: DateTime.UtcNow.AddDays(-7)),
            CreateFullJobInfo(jobId: "old-job", projectId: projectId, pipelineId: "pipeline-2", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddDays(-20), totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "same-job", projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddDays(-1)),
            CreateFullJobInfo(jobId: "same-job", projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow, duration: 1500)
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
            CreateInProgressJobInfo(jobId: "in-progress-job-1", projectId: project1Id, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddMinutes(-5)),
            CreateInProgressJobInfo(jobId: "in-progress-job-2", projectId: project2Id, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2", agentOSName: "windows", startDateTime: DateTime.UtcNow.AddMinutes(-10))
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
            CreateInProgressJobInfo(jobId: "job-main", projectId: projectId, pipelineId: "pipeline-1", branchName: "main"),
            CreateInProgressJobInfo(jobId: "job-develop", projectId: projectId, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2")
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
        var inProgressJob = CreateInProgressJobInfo(jobId: "job-1", jobRunId: sharedJobRunId, projectId: projectId, pipelineId: "pipeline-1");

        await database.InProgressJobInfo.InsertAsync(inProgressJob);

        // Создаем completed job с тем же JobRunId
        var completedJob = CreateFullJobInfo(jobId: "job-1", jobRunId: sharedJobRunId, projectId: projectId, pipelineId: "pipeline-1");

        await database.JobInfo.InsertAsync(completedJob);

        // Act
        var result = await database.FindAllJobsRunsInProgress(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("completed job should be excluded from in-progress jobs");
    }

    [Fact(Skip = "Flaky test, needs investigation")]
    public async Task FindAllJobsRunsInProgress_WithOldJobs_ShouldExcludeJobsOlderThan14Days()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);

        var projectId = $"project-{Guid.NewGuid()}";

        var inProgressJobs = new List<InProgressJobInfo>
        {
            CreateInProgressJobInfo(jobId: "recent-job", projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddDays(-5)),
            CreateInProgressJobInfo(jobId: "old-job", projectId: projectId, pipelineId: "pipeline-2", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddDays(-20))
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
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddDays(-2)),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-2", startDateTime: DateTime.UtcNow.AddDays(-1), duration: 1200, totalTests: 12, successTests: 12),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-3", branchName: "develop", agentName: "agent-2", state: JobStatus.Failed, startDateTime: DateTime.UtcNow.AddHours(-6), duration: 900, totalTests: 8, successTests: 7, failedTests: 1)
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
            CreateFullJobInfo(jobId: "job-main", projectId: projectId, pipelineId: "pipeline-1", branchName: "main"),
            CreateFullJobInfo(jobId: "job-develop", projectId: projectId, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5)
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
            jobs.Add(CreateFullJobInfo(
                jobId: jobId,
                projectId: projectId,
                pipelineId: $"pipeline-{i}",
                branchName: $"branch-{i}",
                startDateTime: DateTime.UtcNow.AddDays(-i - 5)
            ));
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        // Should return max 5 runs per job (rnj <= 5), unless they're within 3 days
        result.Where(r => r.JobId == jobId).Should().HaveCount(5);
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
            jobs.Add(CreateFullJobInfo(
                jobId: jobId,
                projectId: projectId,
                pipelineId: $"pipeline-{i}",
                branchName: $"branch-{i}",
                startDateTime: DateTime.UtcNow.AddHours(-i * 6)
            ));
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
            CreateFullJobInfo(jobId: "recent-job", projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddDays(-7)),
            CreateFullJobInfo(jobId: "old-job", projectId: projectId, pipelineId: "pipeline-2", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddDays(-20), totalTests: 5, successTests: 5)
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

        var job = CreateFullJobInfo(
            jobId: "job-with-commits",
            projectId: projectId,
            pipelineId: "pipeline-1",
            commitSha: "commit1",
            commitMessage: "Latest commit",
            commitAuthor: "Author1",
            changes: changesSinceLastRun
        );

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.FindAllJobsRuns(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(3);
        result[0].ChangesSinceLastRun.Should().HaveCount(3); // arraySlice(, 1, 20)
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
            CreateFullJobInfo(jobId: "job-1", projectId: project1Id, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: "job-2", projectId: project2Id, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddHours(-1), duration: 2000, totalTests: 5, successTests: 5),
            CreateFullJobInfo(jobId: "job-3", projectId: project1Id, pipelineId: "pipeline-3", branchName: "feature/test", startDateTime: DateTime.UtcNow.AddHours(-2), duration: 1500, totalTests: 8, successTests: 8)
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
            CreateFullJobInfo(jobId: job1Id, projectId: projectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: job1Id, projectId: projectId, pipelineId: "pipeline-2", branchName: "develop", startDateTime: DateTime.UtcNow.AddHours(-1)),
            CreateFullJobInfo(jobId: job2Id, projectId: projectId, pipelineId: "pipeline-3", branchName: "feature/other", agentName: "agent-2", duration: 2000, startDateTime: DateTime.UtcNow.AddHours(-2), totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-with-branch", projectId: projectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: "job-without-branch", projectId: projectId, pipelineId: "pipeline-2", branchName: "", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddHours(-1), totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "recent-job", projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddDays(-7)),
            CreateFullJobInfo(jobId: "old-job", projectId: projectId, pipelineId: "pipeline-2", branchName: "old-branch", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddDays(-20), totalTests: 5, successTests: 5)
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

        var job = CreateFullJobInfo(
            jobId: "test-job",
            projectId: projectId,
            pipelineId: "pipeline-1",
            branchName: "test-branch");

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
            CreateFullJobInfo(jobId: "job-1", projectId: projectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: "job-2", projectId: projectId, pipelineId: "pipeline-2", agentName: "agent-2", startDateTime: DateTime.UtcNow.AddHours(-1), totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-1", projectId: projectId, pipelineId: "pipeline-1", branchName: "older-branch", startDateTime: DateTime.UtcNow.AddDays(-5)),
            CreateFullJobInfo(jobId: "job-2", projectId: projectId, pipelineId: "pipeline-2", branchName: "newer-branch", agentName: "agent-2", totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-1", projectId: projectId, pipelineId: pipelineId, commitSha: "abc123"),
            CreateFullJobInfo(jobId: "job-2", projectId: projectId, pipelineId: pipelineId, agentName: "agent-2", duration: 2000, startDateTime: DateTime.UtcNow.AddMinutes(-1), commitSha: "abc123", totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-main", projectId: projectId, pipelineId: "pipeline-main"),
            CreateFullJobInfo(jobId: "job-develop", projectId: projectId, pipelineId: "pipeline-develop", branchName: "develop", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-with-pipeline", projectId: projectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: "job-without-pipeline", projectId: projectId, pipelineId: "", agentName: "agent-2", totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-success", projectId: projectId, pipelineId: pipelineId),
            CreateFullJobInfo(jobId: "job-failed", projectId: projectId, pipelineId: pipelineId, agentName: "agent-2", state: JobStatus.Failed, duration: 500, totalTests: 5, successTests: 4, failedTests: 1)
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
            CreateFullJobInfo(jobId: "job-1", projectId: projectId, pipelineId: pipelineId, customStatusMessage: "Message 1"),
            CreateFullJobInfo(jobId: "job-2", projectId: projectId, pipelineId: pipelineId, agentName: "agent-2", customStatusMessage: "Message 2", totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-old", projectId: projectId, pipelineId: "pipeline-old", startDateTime: DateTime.UtcNow.AddDays(-5)),
            CreateFullJobInfo(jobId: "job-new", projectId: projectId, pipelineId: "pipeline-new", agentName: "agent-2", totalTests: 5, successTests: 5)
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
            jobs.Add(CreateFullJobInfo(
                jobId: $"job-{i}",
                projectId: projectId,
                pipelineId: $"pipeline-{i}",
                startDateTime: DateTime.UtcNow.AddMinutes(-i)));
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(200);
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

        var job = CreateFullJobInfo(
            jobId: "job-with-commits",
            projectId: projectId,
            pipelineId: pipelineId,
            commitSha: "commit1",
            commitMessage: "Latest commit",
            commitAuthor: "Author1",
            changes: changesSinceLastRun);

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetPipelineRunsByProject(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(2);
        result[0].ChangesSinceLastRun.Should().HaveCount(2);
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
            CreateFullJobInfo(jobId: "job-1", projectId: project1Id, pipelineId: "pipeline-1-old", startDateTime: DateTime.UtcNow.AddHours(-2)),
            // Project 1 - main branch - newer run (should be returned)
            CreateFullJobInfo(jobId: "job-2", projectId: project1Id, pipelineId: "pipeline-1-new", duration: 1200, totalTests: 12, successTests: 12),
            // Project 2 - develop branch
            CreateFullJobInfo(jobId: "job-3", projectId: project2Id, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2", state: JobStatus.Failed, duration: 900, startDateTime: DateTime.UtcNow.AddMinutes(-30), totalTests: 8, successTests: 7, failedTests: 1)
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
            CreateFullJobInfo(jobId: "job-main", projectId: projectId, pipelineId: "pipeline-main"),
            CreateFullJobInfo(jobId: "job-develop", projectId: projectId, pipelineId: "pipeline-develop", branchName: "develop", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-with-pipeline", projectId: projectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: "job-without-pipeline", projectId: projectId, pipelineId: "", agentName: "agent-2", totalTests: 5, successTests: 5)
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
            CreateFullJobInfo(jobId: "job-old-main", projectId: projectId, pipelineId: "pipeline-old-main", startDateTime: DateTime.UtcNow.AddDays(-1)),
            // Newer pipeline on main branch
            CreateFullJobInfo(jobId: "job-new-main", projectId: projectId, pipelineId: "pipeline-new-main", duration: 1200, totalTests: 12, successTests: 12),
            // Pipeline on develop branch
            CreateFullJobInfo(jobId: "job-develop", projectId: projectId, pipelineId: "pipeline-develop", branchName: "develop", agentName: "agent-2", duration: 900, startDateTime: DateTime.UtcNow.AddHours(-1), totalTests: 8, successTests: 8)
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
            CreateFullJobInfo(jobId: "job-1", projectId: projectId, pipelineId: pipelineId, commitSha: "abc123"),
            CreateFullJobInfo(jobId: "job-2", projectId: projectId, pipelineId: pipelineId, agentName: "agent-2", duration: 2000, startDateTime: DateTime.UtcNow.AddMinutes(-1), commitSha: "abc123", totalTests: 5, successTests: 5)
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
            jobs.Add(CreateFullJobInfo(
                jobId: $"job-{i}",
                projectId: projectId,
                pipelineId: $"pipeline-{i}",
                branchName: $"branch-{i}",
                startDateTime: DateTime.UtcNow.AddMinutes(-i)));
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1000);
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
            CreateFullJobInfo(jobId: "job-old", projectId: projectId, pipelineId: "pipeline-old", branchName: "branch-old", startDateTime: DateTime.UtcNow.AddDays(-5)),
            CreateFullJobInfo(jobId: "job-new", projectId: projectId, pipelineId: "pipeline-new", branchName: "branch-new", agentName: "agent-2", totalTests: 5, successTests: 5)
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

        var job = CreateFullJobInfo(
            jobId: "job-with-commits",
            projectId: projectId,
            pipelineId: pipelineId,
            commitSha: "commit1",
            commitMessage: "Latest commit",
            commitAuthor: "Author1",
            changes: changesSinceLastRun);

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetPipelineRunsOverview(new[] { projectId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(3);
        result[0].ChangesSinceLastRun.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFlakyTestsCount_WithFlakyTests_ShouldReturnCorrectCount()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        // Insert flaky test with flip rate > 0.1
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-1", runCount: 100, flipCount: 15);
        // Insert another flaky test
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-2", runCount: 50, flipCount: 10);
        // Insert stable test (flip rate < 0.1)
        await InsertTestDashboardEntry(projectId, jobId, "stable-test", runCount: 100, flipCount: 5);
        // Insert test with low run count (should be excluded)
        await InsertTestDashboardEntry(projectId, jobId, "low-runs-test", runCount: 10, flipCount: 5);

        // Act
        var result = await database.GetFlakyTestsCount(projectId, jobId);

        // Assert
        result.Should().Be(2, "should count only tests with RunCount > 20 and FlipRate > 0.1");
    }

    [Fact]
    public async Task GetFlakyTestsCount_WithNoFlakyTests_ShouldReturnZero()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-no-flaky";

        // Insert only stable tests
        await InsertTestDashboardEntry(projectId, jobId, "stable-test-1", runCount: 100, flipCount: 5);
        await InsertTestDashboardEntry(projectId, jobId, "stable-test-2", runCount: 50, flipCount: 2);

        // Act
        var result = await database.GetFlakyTestsCount(projectId, jobId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetFlakyTestsCount_WithNonExistentJob_ShouldReturnZero()
    {
        // Arrange - no data inserted

        // Act
        var result = await database.GetFlakyTestsCount("non-existent-project", "non-existent-job");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetFlakyTestsCount_WithCustomFlipRateThreshold_ShouldFilterCorrectly()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-custom-threshold";

        // Insert test with flip rate = 0.15 (15%)
        await InsertTestDashboardEntry(projectId, jobId, "medium-flaky-test", runCount: 100, flipCount: 15);
        // Insert test with flip rate = 0.25 (25%)
        await InsertTestDashboardEntry(projectId, jobId, "high-flaky-test", runCount: 100, flipCount: 25);

        // Act - use custom threshold of 0.2 (20%)
        var result = await database.GetFlakyTestsCount(projectId, jobId, flipRateThreshold: 0.2);

        // Assert
        result.Should().Be(1, "should count only tests with FlipRate > 0.2");
    }

    [Fact]
    public async Task GetFlakyTestsCount_WithOldTests_ShouldExcludeTestsOlderThan7Days()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-old-tests";

        // Insert recent flaky test
        await InsertTestDashboardEntry(projectId, jobId, "recent-flaky-test", runCount: 100, flipCount: 15, lastRunDate: DateTime.UtcNow.AddDays(-3));
        // Insert old flaky test (should be excluded)
        await InsertTestDashboardEntry(projectId, jobId, "old-flaky-test", runCount: 100, flipCount: 20, lastRunDate: DateTime.UtcNow.AddDays(-10));

        // Act
        var result = await database.GetFlakyTestsCount(projectId, jobId);

        // Assert
        result.Should().Be(1, "should exclude tests with LastRunDate older than 7 days");
    }

    [Fact]
    public async Task GetFlakyTestsCount_WithMultipleUpdates_ShouldUseLatestData()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-updates";
        var testId = "updated-test";

        // Insert initial data (not flaky)
        await InsertTestDashboardEntry(projectId, jobId, testId, runCount: 50, flipCount: 3, updatedAt: DateTime.UtcNow.AddHours(-2));

        // Insert updated data (now flaky) - ReplacingMergeTree should use this
        await InsertTestDashboardEntry(projectId, jobId, testId, runCount: 100, flipCount: 15, updatedAt: DateTime.UtcNow);

        // Act
        var result = await database.GetFlakyTestsCount(projectId, jobId);

        // Assert
        result.Should().Be(1, "should use the latest data based on UpdatedAt");
    }

    private async Task InsertTestDashboardEntry(
        string projectId,
        string jobId,
        string testId,
        ulong runCount,
        ulong flipCount,
        DateTime? lastRunDate = null,
        DateTime? updatedAt = null)
    {
        await using var connection = connectionFactory.CreateConnection();

        var lastRun = lastRunDate ?? DateTime.UtcNow;
        var updated = updatedAt ?? DateTime.UtcNow;

        var query = $@"
            INSERT INTO TestDashboardWeekly (ProjectId, JobId, TestId, LastRunDate, RunCount, FailCount, FlipCount, UpdatedAt)
            VALUES ('{projectId}', '{jobId}', '{testId}', '{lastRun:yyyy-MM-dd}', {runCount}, 0, {flipCount}, '{updated:yyyy-MM-dd HH:mm:ss}')
        ";

        await connection.ExecuteQueryAsync(query);
    }

    [Fact]
    public async Task GetJobInfo_WithExistingJob_ShouldReturnCompleteJobInfo()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-getinfo";

        var changesSinceLastRun = new List<CommitParentsChangesEntry>
        {
            new() { ParentCommitSha = "commit1", Depth = 0, AuthorName = "Author1", AuthorEmail = "author1@test.com", MessagePreview = "Message 1" },
            new() { ParentCommitSha = "commit2", Depth = 1, AuthorName = "Author2", AuthorEmail = "author2@test.com", MessagePreview = "Message 2" }
        };

        var job = CreateFullJobInfo(
            jobId: jobId,
            projectId: projectId,
            pipelineId: "pipeline-123",
            commitSha: "abc123",
            commitMessage: "Test commit message",
            commitAuthor: "Test Author",
            changes: changesSinceLastRun,
            triggered: "push",
            pipelineSource: "push",
            customStatusMessage: "Build successful");

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, jobId, job.JobRunId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.JobRunId.Should().Be(job.JobRunId);
        result.ProjectId.Should().Be(projectId);
        result.BranchName.Should().Be(job.BranchName);
        result.AgentName.Should().Be(job.AgentName);
        result.AgentOSName.Should().Be(job.AgentOSName);
        result.StartDateTime.Should().BeCloseTo(job.StartDateTime, TimeSpan.FromSeconds(1));
        result.EndDateTime.Should().BeCloseTo(job.EndDateTime, TimeSpan.FromSeconds(1));
        result.TotalTestsCount.Should().Be(job.TotalTestsCount);
        result.Duration.Should().Be(job.Duration);
        result.SuccessTestsCount.Should().Be(job.SuccessTestsCount);
        result.SkippedTestsCount.Should().Be(job.SkippedTestsCount);
        result.FailedTestsCount.Should().Be(job.FailedTestsCount);
        result.State.Should().Be("Success");
        result.CustomStatusMessage.Should().Be("Build successful");
        result.JobUrl.Should().Be(job.JobUrl);
        result.PipelineSource.Should().Be("push");
        result.Triggered.Should().Be("push");
        result.HasCodeQualityReport.Should().BeFalse();
        result.PipelineId.Should().Be("pipeline-123");
        result.CommitSha.Should().Be("abc123");
        result.CommitMessage.Should().Be("Test commit message");
        result.CommitAuthor.Should().Be("Test Author");
        result.TotalCoveredCommitCount.Should().Be(2);
        result.ChangesSinceLastRun.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetJobInfo_WithNonExistentJob_ShouldReturnNull()
    {
        // Arrange - no data

        // Act
        var result = await database.GetJobInfo("non-existent-project", "non-existent-job", "non-existent-jobrun");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobInfo_WithWrongProjectId_ShouldReturnNull()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var wrongProjectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        var job = CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1");
        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(wrongProjectId, jobId, job.JobRunId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobInfo_WithWrongJobId_ShouldReturnNull()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        var job = CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1");
        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, "wrong-job-id", job.JobRunId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobInfo_WithWrongJobRunId_ShouldReturnNull()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        var job = CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1");
        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, jobId, "wrong-jobrun-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobInfo_WithFailedJob_ShouldReturnCorrectState()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-failed";

        var job = CreateFullJobInfo(
            jobId: jobId,
            projectId: projectId,
            pipelineId: "pipeline-1",
            state: JobStatus.Failed,
            totalTests: 10,
            successTests: 8,
            failedTests: 2);

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, jobId, job.JobRunId);

        // Assert
        result.Should().NotBeNull();
        result!.State.Should().Be("Failed");
        result.FailedTestsCount.Should().Be(2);
        result.SuccessTestsCount.Should().Be(8);
    }

    [Fact]
    public async Task GetJobInfo_WithManyCommits_ShouldLimitTo20()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-many-commits";

        var changesSinceLastRun = new List<CommitParentsChangesEntry>();
        for (int i = 0; i < 50; i++)
        {
            changesSinceLastRun.Add(new CommitParentsChangesEntry
            {
                ParentCommitSha = $"commit-{i}",
                Depth = (ushort)i,
                AuthorName = $"Author{i}",
                AuthorEmail = $"author{i}@test.com",
                MessagePreview = $"Message {i}"
            });
        }

        var job = CreateFullJobInfo(
            jobId: jobId,
            projectId: projectId,
            pipelineId: "pipeline-1",
            changes: changesSinceLastRun);

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, jobId, job.JobRunId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalCoveredCommitCount.Should().Be(50, "should report total number of commits");
        result.ChangesSinceLastRun.Should().HaveCount(20, "should limit ChangesSinceLastRun to first 20 commits");
        result.ChangesSinceLastRun[0].Item1.Should().Be("commit-0");
        result.ChangesSinceLastRun[19].Item1.Should().Be("commit-19");
    }

    [Fact]
    public async Task GetJobInfo_WithNoCommits_ShouldReturnEmptyCommitArray()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-no-commits";

        var job = CreateFullJobInfo(
            jobId: jobId,
            projectId: projectId,
            pipelineId: "pipeline-1",
            changes: new List<CommitParentsChangesEntry>());

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, jobId, job.JobRunId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalCoveredCommitCount.Should().Be(0);
        result.ChangesSinceLastRun.Should().BeEmpty();
    }

    [Fact]
    public async Task GetJobInfo_WithCodeQualityReport_ShouldReturnTrue()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-quality";

        var job = CreateFullJobInfo(
            jobId: jobId,
            projectId: projectId,
            pipelineId: "pipeline-1",
            hasCodeQualityReport: true);

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.GetJobInfo(projectId, jobId, job.JobRunId);

        // Assert
        result.Should().NotBeNull();
        result!.HasCodeQualityReport.Should().BeTrue();
    }


    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithCompletedRuns_ShouldReturnAllRuns()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddDays(-1)),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-2", startDateTime: DateTime.UtcNow, duration: 1500, totalTests: 12, successTests: 12)
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].StartDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10), "should be ordered by StartDateTime DESC");
        result[1].StartDateTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(10));
        result.All(r => r.JobId == jobId && r.ProjectId == projectId).Should().BeTrue();
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithInProgressRuns_ShouldIncludeThemWithNullFields()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-inprogress";

        var inProgressJob = CreateInProgressJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1");
        await database.InProgressJobInfo.InsertAsync(inProgressJob);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var inProgressRun = result[0];
        inProgressRun.JobId.Should().Be(jobId);
        inProgressRun.State.Should().Be("Running");
        inProgressRun.TotalTestsCount.Should().BeNull("in-progress jobs should have null test counts");
        inProgressRun.Duration.Should().BeNull("in-progress jobs should have null duration");
        inProgressRun.SuccessTestsCount.Should().BeNull();
        inProgressRun.SkippedTestsCount.Should().BeNull();
        inProgressRun.FailedTestsCount.Should().BeNull();
        inProgressRun.CustomStatusMessage.Should().Be("");
        inProgressRun.HasCodeQualityReport.Should().BeFalse();
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithMixedRuns_ShouldReturnBothInProgressAndCompleted()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-mixed";

        var completedJob = CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1", startDateTime: DateTime.UtcNow.AddHours(-2));
        await database.JobInfo.InsertAsync(completedJob);

        var inProgressJob = CreateInProgressJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-2", startDateTime: DateTime.UtcNow.AddMinutes(-10));
        await database.InProgressJobInfo.InsertAsync(inProgressJob);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var inProgressRun = result.First(r => r.State == "Running");
        inProgressRun.Should().NotBeNull();
        inProgressRun.TotalTestsCount.Should().BeNull();

        var completedRun = result.First(r => r.State != "Running");
        completedRun.Should().NotBeNull();
        completedRun.TotalTestsCount.Should().Be(10);
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_ShouldExcludeCompletedInProgressJobs()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-completed-inprogress";
        var sharedJobRunId = Guid.NewGuid().ToString();

        // Create in-progress job
        var inProgressJob = CreateInProgressJobInfo(jobId: jobId, jobRunId: sharedJobRunId, projectId: projectId, pipelineId: "pipeline-1");
        await database.InProgressJobInfo.InsertAsync(inProgressJob);

        // Create completed job with same JobRunId
        var completedJob = CreateFullJobInfo(jobId: jobId, jobRunId: sharedJobRunId, projectId: projectId, pipelineId: "pipeline-1");
        await database.JobInfo.InsertAsync(completedJob);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1, "should exclude in-progress job that has been completed");
        result[0].State.Should().NotBe("Running");
        result[0].TotalTestsCount.Should().NotBeNull();
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-branches";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1", branchName: "main"),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-3", branchName: "feature/test", startDateTime: DateTime.UtcNow.AddHours(-1), totalTests: 8, successTests: 8)
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId, "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].BranchName.Should().Be("main");
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-pagination";

        var jobs = new List<FullJobInfo>();
        for (int i = 0; i < 150; i++)
        {
            jobs.Add(CreateFullJobInfo(
                jobId: jobId,
                projectId: projectId,
                pipelineId: $"pipeline-{i}",
                startDateTime: DateTime.UtcNow.AddMinutes(-i)));
        }

        await database.JobInfo.InsertAsync(jobs);

        // Act - Get first page
        var page0 = await database.FindAllJobsRunsPerJobId(projectId, jobId, page: 0);

        // Act - Get second page
        var page1 = await database.FindAllJobsRunsPerJobId(projectId, jobId, page: 1);

        // Assert
        page0.Should().NotBeNull();
        page0.Should().HaveCount(100, "should return 100 items per page");

        page1.Should().NotBeNull();
        page1.Should().HaveCount(50, "second page should have remaining 50 items");

        // Verify pages don't overlap
        var page0Ids = page0.Select(r => r.JobRunId).ToHashSet();
        var page1Ids = page1.Select(r => r.JobRunId).ToHashSet();
        page0Ids.Intersect(page1Ids).Should().BeEmpty("pages should not contain duplicate JobRunIds");
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_ShouldOrderByStartDateTimeDesc()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-ordering";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-old", startDateTime: DateTime.UtcNow.AddDays(-5)),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-middle", startDateTime: DateTime.UtcNow.AddDays(-2), duration: 1200, totalTests: 12, successTests: 12),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-new", startDateTime: DateTime.UtcNow, duration: 900, totalTests: 8, successTests: 8)
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].StartDateTime.Should().BeAfter(result[1].StartDateTime);
        result[1].StartDateTime.Should().BeAfter(result[2].StartDateTime);
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithNonExistentJob_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.FindAllJobsRunsPerJobId("non-existent-project", "non-existent-job");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithDifferentJobId_ShouldNotReturnOtherJobs()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var targetJobId = "target-job";
        var otherJobId = "other-job";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: targetJobId, projectId: projectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: otherJobId, projectId: projectId, pipelineId: "pipeline-2", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5)
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, targetJobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].JobId.Should().Be(targetJobId);
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithDifferentProjectId_ShouldNotReturnOtherProjects()
    {
        // Arrange
        var targetProjectId = $"project-{Guid.NewGuid()}";
        var otherProjectId = $"project-{Guid.NewGuid()}";
        var jobId = "shared-job-id";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: jobId, projectId: targetProjectId, pipelineId: "pipeline-1"),
            CreateFullJobInfo(jobId: jobId, projectId: otherProjectId, pipelineId: "pipeline-2", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5)
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(targetProjectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProjectId.Should().Be(targetProjectId);
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_ShouldHandleChangesSinceLastRun()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-commits";

        var changes = new List<CommitParentsChangesEntry>();
        // Create more than 20 commits to test arraySlice
        for (int i = 0; i < 30; i++)
        {
            changes.Add(new CommitParentsChangesEntry
            {
                ParentCommitSha = $"commit-{i}",
                Depth = (ushort)i,
                AuthorName = $"Author{i}",
                AuthorEmail = $"author{i}@test.com",
                MessagePreview = $"Message {i}"
            });
        }

        var job = CreateFullJobInfo(
            jobId: jobId,
            projectId: projectId,
            pipelineId: "pipeline-1",
            commitSha: "commit-0",
            commitMessage: "Latest commit",
            commitAuthor: "Author0",
            changes: changes);

        await database.JobInfo.InsertAsync(job);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TotalCoveredCommitCount.Should().Be(30, "should report total number of commits");
        result[0].ChangesSinceLastRun.Should().HaveCount(20, "should limit ChangesSinceLastRun to first 20 commits");
        result[0].ChangesSinceLastRun[0].ParentCommitSha.Should().Be("commit-0");
        result[0].ChangesSinceLastRun[19].ParentCommitSha.Should().Be("commit-19");
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithInProgressAndBranchFilter_ShouldFilterInProgressJobs()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-inprogress-filter";

        var inProgressMainJob = CreateInProgressJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1", branchName: "main");
        var inProgressDevelopJob = CreateInProgressJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2");

        await database.InProgressJobInfo.InsertAsync(inProgressMainJob);
        await database.InProgressJobInfo.InsertAsync(inProgressDevelopJob);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId, "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].BranchName.Should().Be("main");
        result[0].State.Should().Be("Running");
    }

    [Fact]
    public async Task FindAllJobsRunsPerJobId_WithCompletedJobsHavingDifferentStates_ShouldReturnAllStates()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job-states";

        var jobs = new List<FullJobInfo>
        {
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-1", state: JobStatus.Success),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-2", state: JobStatus.Failed, startDateTime: DateTime.UtcNow.AddHours(-1), duration: 1200, totalTests: 12, successTests: 10, failedTests: 2),
            CreateFullJobInfo(jobId: jobId, projectId: projectId, pipelineId: "pipeline-3", state: JobStatus.Canceled, startDateTime: DateTime.UtcNow.AddHours(-2), duration: 900, totalTests: 8, successTests: 8)
        };

        await database.JobInfo.InsertAsync(jobs);

        // Act
        var result = await database.FindAllJobsRunsPerJobId(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.State == "Success");
        result.Should().Contain(r => r.State == "Failed");
        result.Should().Contain(r => r.State == "Canceled");
    }

    [Fact]
    public async Task GetTestList_WithBasicTests_ShouldReturnCorrectResults()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Test2", TestResult.Failed, duration: 3000L),
            CreateTestRun("Test3", TestResult.Skipped, duration: 0L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(jobRunInfo.ProjectId, jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.TestId == "Test1" && t.FinalState == "Success");
        result.Should().Contain(t => t.TestId == "Test2" && t.FinalState == "Failed");
        result.Should().Contain(t => t.TestId == "Test3" && t.FinalState == "Skipped");
    }

    [Fact]
    public async Task GetTestList_WithMultipleJobRuns_ShouldAggregateCorrectly()
    {
        // Arrange
        var jobRunInfo1 = CreateJobRunInfo();
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobRunInfo1.JobId, projectId: jobRunInfo1.ProjectId);

        var testId = $"SharedTest-{Guid.NewGuid()}";

        var tests1 = new[]
        {
            CreateTestRun(testId, TestResult.Success, duration: 5000L, startDateTime: DateTime.UtcNow.AddHours(-2))
        };

        var tests2 = new[]
        {
            CreateTestRun(testId, TestResult.Success, duration: 3000L, startDateTime: DateTime.UtcNow.AddHours(-1))
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);

        // Act
        var result = await database.GetTestList(jobRunInfo1.ProjectId, jobRunInfo1.JobId, new[] { jobRunInfo1.JobRunId, jobRunInfo2.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var test = result[0];
        test.TestId.Should().Be(testId);
        test.FinalState.Should().Be("Success");
        test.TotalRuns.Should().Be(2);
        test.AvgDuration.Should().Be(4000); // (5000 + 3000) / 2
        test.MinDuration.Should().Be(3000);
        test.MaxDuration.Should().Be(5000);
        test.AllStates.Should().Be("Success,Success");
    }

    [Fact]
    public async Task GetTestList_WithMixedStates_ShouldReturnSuccessAsFinalState()
    {
        // Arrange
        var jobRunInfo1 = CreateJobRunInfo();
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobRunInfo1.JobId, projectId: jobRunInfo1.ProjectId);

        var testId = $"FlakyTest-{Guid.NewGuid()}";

        var tests1 = new[]
        {
            CreateTestRun(testId, TestResult.Failed, duration: 5000L)
        };

        var tests2 = new[]
        {
            CreateTestRun(testId, TestResult.Success, duration: 3000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);

        // Act
        var result = await database.GetTestList(jobRunInfo1.ProjectId, jobRunInfo1.JobId, new[] { jobRunInfo1.JobRunId, jobRunInfo2.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var test = result[0];
        test.TestId.Should().Be(testId);
        test.FinalState.Should().Be("Success", "if at least one run was successful, final state should be Success");
        test.AllStates.Should().Contain("Failed");
        test.AllStates.Should().Contain("Success");
    }

    [Fact]
    public async Task GetTestList_WithStateFilter_ShouldReturnOnlyMatchingTests()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("SuccessTest1", TestResult.Success),
            CreateTestRun("SuccessTest2", TestResult.Success),
            CreateTestRun("FailedTest1", TestResult.Failed)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            testStateFilter: "Success");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.FinalState.Should().Be("Success"));
    }

    [Fact]
    public async Task GetTestList_WithTestIdQuery_ShouldFilterByTestId()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("MyApp.Tests.UnitTest1", TestResult.Success),
            CreateTestRun("MyApp.Tests.UnitTest2", TestResult.Success),
            CreateTestRun("MyApp.Tests.IntegrationTest1", TestResult.Failed)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            testIdQuery: "UnitTest");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.TestId.Should().Contain("UnitTest"));
    }

    [Fact]
    public async Task GetTestList_WithSorting_ShouldSortCorrectly()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test_C", TestResult.Success, duration: 1000L),
            CreateTestRun("Test_A", TestResult.Success, duration: 3000L),
            CreateTestRun("Test_B", TestResult.Success, duration: 2000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act - Sort by TestId ASC
        var resultByTestId = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            sortField: "TestId",
            sortDirection: "ASC");

        // Assert
        resultByTestId.Should().NotBeNull();
        resultByTestId.Should().HaveCount(3);
        resultByTestId[0].TestId.Should().Be("Test_A");
        resultByTestId[1].TestId.Should().Be("Test_B");
        resultByTestId[2].TestId.Should().Be("Test_C");

        // Act - Sort by Duration DESC
        var resultByDuration = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            sortField: "Duration",
            sortDirection: "DESC");

        // Assert
        resultByDuration.Should().NotBeNull();
        resultByDuration[0].TestId.Should().Be("Test_A");
        resultByDuration[2].TestId.Should().Be("Test_C");
    }

    [Fact]
    public async Task GetTestList_WithDefaultSorting_ShouldSortByStateWeight()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("SkippedTest", TestResult.Skipped), // Weight: 0
            CreateTestRun("SuccessTest", TestResult.Success), // Weight: 1
            CreateTestRun("FailedTest", TestResult.Failed)    // Weight: 100
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(jobRunInfo.ProjectId, jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].TestId.Should().Be("FailedTest", "failed tests should come first (highest weight)");
        result[1].TestId.Should().Be("SuccessTest");
        result[2].TestId.Should().Be("SkippedTest", "skipped tests should come last (lowest weight)");
    }

    [Fact]
    public async Task GetTestList_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new List<TestRun>();
        for (int i = 0; i < 150; i++)
        {
            tests.Add(CreateTestRun($"Test{i:D3}", TestResult.Success));
        }

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act - Get first page (default 100 items)
        var page0 = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            sortField: "TestId",
            sortDirection: "ASC");

        // Act - Get second page
        var page1 = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            sortField: "TestId",
            sortDirection: "ASC",
            page: 1);

        // Assert
        page0.Should().NotBeNull();
        page0.Should().HaveCount(100, "should return 100 items per page by default");

        page1.Should().NotBeNull();
        page1.Should().HaveCount(50, "second page should have remaining 50 items");

        // Verify pages don't overlap
        var page0Ids = page0.Select(r => r.TestId).ToHashSet();
        var page1Ids = page1.Select(r => r.TestId).ToHashSet();
        page0Ids.Intersect(page1Ids).Should().BeEmpty("pages should not contain duplicate TestIds");
    }

    [Fact]
    public async Task GetTestList_WithCustomItemsPerPage_ShouldRespectLimit()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new List<TestRun>();
        for (int i = 0; i < 50; i++)
        {
            tests.Add(CreateTestRun($"Test{i:D2}", TestResult.Success));
        }

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            itemsPerPage: 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetTestList_WithNoMatchingJobRuns_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestList("test-project", "test-job", new[] { "non-existent-jobrun" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestList_WithEmptyJobRunIds_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestList("test-project", "test-job", Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestList_WithSpecialCharactersInTestId_ShouldHandleCorrectly()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test's with 'quotes'", TestResult.Success),
            CreateTestRun("Test with % wildcard", TestResult.Failed),
            CreateTestRun("Test_with_underscores", TestResult.Success)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(jobRunInfo.ProjectId, jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.TestId == "Test's with 'quotes'");
        result.Should().Contain(t => t.TestId == "Test with % wildcard");
    }

    [Fact]
    public async Task GetTestList_WithSpecialCharactersInQuery_ShouldEscapeCorrectly()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test's with 'quotes'", TestResult.Success),
            CreateTestRun("Regular test", TestResult.Success)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestList(
            jobRunInfo.ProjectId,
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            testIdQuery: "quotes");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TestId.Should().Be("Test's with 'quotes'");
    }

    [Fact]
    public async Task GetTestListStats_WithBasicTests_ShouldReturnCorrectCounts()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Test2", TestResult.Success, duration: 4000L),
            CreateTestRun("Test3", TestResult.Failed, duration: 3000L),
            CreateTestRun("Test4", TestResult.Skipped, duration: 0L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListStats("project-id", jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(4);
        result.SuccessTestsCount.Should().Be(2);
        result.FailedTestsCount.Should().Be(1);
        result.SkippedTestsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTestListStats_WithMultipleJobRuns_ShouldCountTestsOnce()
    {
        // Arrange
        var jobRunInfo1 = CreateJobRunInfo();
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobRunInfo1.JobId, projectId: jobRunInfo1.ProjectId);

        var testId = $"SharedTest-{Guid.NewGuid()}";

        // Same test runs in different job runs - should be counted as 1 test with Success state
        var tests1 = new[]
        {
            CreateTestRun(testId, TestResult.Failed, duration: 5000L)
        };

        var tests2 = new[]
        {
            CreateTestRun(testId, TestResult.Success, duration: 3000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);

        // Act
        var result = await database.GetTestListStats("project-id", jobRunInfo1.JobId, new[] { jobRunInfo1.JobRunId, jobRunInfo2.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(1, "same test in multiple job runs should be counted once");
        result.SuccessTestsCount.Should().Be(1, "if test succeeded in any run, it counts as success");
        result.FailedTestsCount.Should().Be(0);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithStateFilter_ShouldCountOnlyMatchingTests()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Test2", TestResult.Failed, duration: 3000L),
            CreateTestRun("Test3", TestResult.Failed, duration: 2000L),
            CreateTestRun("Test4", TestResult.Skipped, duration: 0L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListStats(
            "project-id",
            jobRunInfo.JobId,
            [jobRunInfo.JobRunId],
            testStateFilter: "Failed");

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(2, "should count only failed tests");
        result.FailedTestsCount.Should().Be(2);
        result.SuccessTestsCount.Should().Be(0);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithTestIdQuery_ShouldFilterByTestId()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Integration.Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Integration.Test2", TestResult.Failed, duration: 3000L),
            CreateTestRun("Unit.Test1", TestResult.Success, duration: 1000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListStats(
            "project-id",
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            testIdQuery: "Integration");

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(2, "should count only tests matching 'Integration'");
        result.SuccessTestsCount.Should().Be(1);
        result.FailedTestsCount.Should().Be(1);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithNoMatchingTests_ShouldReturnZeroCounts()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test1", TestResult.Success, duration: 5000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListStats(
            "project-id",
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            testStateFilter: "Failed");

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(0);
        result.SuccessTestsCount.Should().Be(0);
        result.FailedTestsCount.Should().Be(0);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithEmptyJobRunIds_ShouldReturnZeroCounts()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestListStats("project-id", "test-job", Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(0);
        result.SuccessTestsCount.Should().Be(0);
        result.FailedTestsCount.Should().Be(0);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithNonExistentJobRun_ShouldReturnZeroCounts()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestListStats("project-id", "test-job", new[] { "non-existent-jobrun" });

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(0);
        result.SuccessTestsCount.Should().Be(0);
        result.FailedTestsCount.Should().Be(0);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithMixedStatesAcrossRuns_ShouldUseFinalState()
    {
        // Arrange
        var jobRunInfo1 = CreateJobRunInfo();
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobRunInfo1.JobId, projectId: jobRunInfo1.ProjectId);

        var test1 = $"Test1-{Guid.NewGuid()}";
        var test2 = $"Test2-{Guid.NewGuid()}";

        // Test1: Failed -> Success (final state: Success)
        var tests1 = new[]
        {
            CreateTestRun(test1, TestResult.Failed, duration: 5000L),
            CreateTestRun(test2, TestResult.Success, duration: 3000L)
        };

        var tests2 = new[]
        {
            CreateTestRun(test1, TestResult.Success, duration: 3000L),
            CreateTestRun(test2, TestResult.Success, duration: 3500L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);

        // Act
        var result = await database.GetTestListStats("project-id", jobRunInfo1.JobId, new[] { jobRunInfo1.JobRunId, jobRunInfo2.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(2);
        result.SuccessTestsCount.Should().Be(2, "both tests have at least one success run");
        result.FailedTestsCount.Should().Be(0);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListStats_WithCombinedFilters_ShouldApplyBothFilters()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Integration.Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Integration.Test2", TestResult.Failed, duration: 3000L),
            CreateTestRun("Unit.Test1", TestResult.Failed, duration: 1000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListStats(
            "project-id",
            jobRunInfo.JobId,
            new[] { jobRunInfo.JobRunId },
            testIdQuery: "Integration",
            testStateFilter: "Failed");

        // Assert
        result.Should().NotBeNull();
        result!.TotalTestsCount.Should().Be(1, "should count only Integration tests with Failed state");
        result.SuccessTestsCount.Should().Be(0);
        result.FailedTestsCount.Should().Be(1);
        result.SkippedTestsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTestStats_WithExistingTests_ShouldReturnCorrectStats()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfo1 = CreateJobRunInfo(jobId: jobId, projectId: projectId);
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobId, projectId: projectId);
        var jobRunInfo3 = CreateJobRunInfo(jobId: jobId, projectId: projectId);

        var tests1 = new[] { CreateTestRun(testId, TestResult.Success, duration: 5000L, startDateTime: DateTime.UtcNow.AddHours(-3)) };
        var tests2 = new[] { CreateTestRun(testId, TestResult.Failed, duration: 3000L, startDateTime: DateTime.UtcNow.AddHours(-2)) };
        var tests3 = new[] { CreateTestRun(testId, TestResult.Success, duration: 4000L, startDateTime: DateTime.UtcNow.AddHours(-1)) };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);
        await database.TestRuns.InsertBatchAsync(jobRunInfo3, tests3);

        // Act
        var result = await database.GetTestStats(testId, new[] { jobId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].State.Should().Be("Success");
        result[0].Duration.Should().Be(4000);
        result[1].State.Should().Be("Failed");
        result[1].Duration.Should().Be(3000);
        result[2].State.Should().Be("Success");
        result[2].Duration.Should().Be(5000);
    }

    [Fact]
    public async Task GetTestStats_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfoMain = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "main");
        var jobRunInfoDevelop = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "develop");

        var testsMain = new[] { CreateTestRun(testId, TestResult.Success, duration: 5000L) };
        var testsDevelop = new[] { CreateTestRun(testId, TestResult.Failed, duration: 3000L) };

        await database.TestRuns.InsertBatchAsync(jobRunInfoMain, testsMain);
        await database.TestRuns.InsertBatchAsync(jobRunInfoDevelop, testsDevelop);

        // Act
        var result = await database.GetTestStats(testId, new[] { jobId }, branchName: "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].State.Should().Be("Success");
        result[0].Duration.Should().Be(5000);
    }

    [Fact]
    public async Task GetTestStats_WithMultipleJobIds_ShouldReturnFromAllJobs()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId1 = "test-job-1";
        var jobId2 = "test-job-2";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfo1 = CreateJobRunInfo(jobId: jobId1, projectId: projectId);
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobId2, projectId: projectId);

        var tests1 = new[] { CreateTestRun(testId, TestResult.Success, duration: 5000L) };
        var tests2 = new[] { CreateTestRun(testId, TestResult.Failed, duration: 3000L) };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);

        // Act
        var result = await database.GetTestStats(testId, new[] { jobId1, jobId2 });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.State == "Success" && r.Duration == 5000);
        result.Should().Contain(r => r.State == "Failed" && r.Duration == 3000);
    }

    [Fact]
    public async Task GetTestStats_ShouldLimitTo1000Results()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        // Create 1100 test runs
        for (int i = 0; i < 1100; i++)
        {
            var jobRunInfo = CreateJobRunInfo(jobId: jobId, projectId: projectId);
            var tests = new[] { CreateTestRun(testId, TestResult.Success, duration: (i + 1) * 100L, startDateTime: DateTime.UtcNow.AddHours(-i)) };
            await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);
        }

        // Act
        var result = await database.GetTestStats(testId, new[] { jobId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1000, "should limit results to 1000");
    }

    [Fact]
    public async Task GetTestStats_WithNonExistentTest_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestStats("non-existent-test", new[] { "non-existent-job" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFlakyTests_WithFlakyTests_ShouldReturnCorrectResults()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        // Insert flaky test with flip rate > 0.1
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-1", runCount: 100, flipCount: 15);
        // Insert another flaky test
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-2", runCount: 50, flipCount: 10);
        // Insert stable test (flip rate < 0.1)
        await InsertTestDashboardEntry(projectId, jobId, "stable-test", runCount: 100, flipCount: 5);

        // Act
        var result = await database.GetFlakyTests(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.TestId == "flaky-test-1");
        result.Should().Contain(t => t.TestId == "flaky-test-2");
        result.Should().NotContain(t => t.TestId == "stable-test");

        var flakyTest1 = result.First(t => t.TestId == "flaky-test-1");
        flakyTest1.RunCount.Should().Be(100);
        flakyTest1.FlipCount.Should().Be(15);
        flakyTest1.FlipRate.Should().BeApproximately(0.15, 0.01);
    }

    [Fact]
    public async Task GetFlakyTests_ShouldOrderByFlipRateDesc()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-low", runCount: 100, flipCount: 12);
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-high", runCount: 100, flipCount: 30);
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-medium", runCount: 100, flipCount: 20);

        // Act
        var result = await database.GetFlakyTests(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].TestId.Should().Be("flaky-test-high", "should order by flip rate descending");
        result[1].TestId.Should().Be("flaky-test-medium");
        result[2].TestId.Should().Be("flaky-test-low");
    }

    [Fact]
    public async Task GetFlakyTests_WithLimitAndOffset_ShouldPaginateCorrectly()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        for (int i = 0; i < 25; i++)
        {
            await InsertTestDashboardEntry(projectId, jobId, $"flaky-test-{i:D2}", runCount: 100, flipCount: (ulong)(15 + i)); // Different flip counts to ensure consistent ordering
        }

        // Act - First page with limit 10
        var page1 = await database.GetFlakyTests(projectId, jobId, limit: 10, offset: 0);

        // Act - Second page with limit 10
        var page2 = await database.GetFlakyTests(projectId, jobId, limit: 10, offset: 10);

        // Assert
        page1.Should().NotBeNull();
        page1.Should().HaveCount(10);

        page2.Should().NotBeNull();
        page2.Should().HaveCount(10);

        // Verify total count
        var allResults = await database.GetFlakyTests(projectId, jobId, limit: 100, offset: 0);
        allResults.Should().HaveCount(25);
    }

    [Fact]
    public async Task GetFlakyTests_WithCustomFlipRateThreshold_ShouldFilterCorrectly()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        await InsertTestDashboardEntry(projectId, jobId, "test-low-flip", runCount: 100, flipCount: 15); // 0.15
        await InsertTestDashboardEntry(projectId, jobId, "test-high-flip", runCount: 100, flipCount: 25); // 0.25

        // Act - Use threshold of 0.2
        var result = await database.GetFlakyTests(projectId, jobId, flipRateThreshold: 0.2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TestId.Should().Be("test-high-flip");
    }

    [Fact]
    public async Task GetFlakyTests_WithLowRunCount_ShouldExcludeTests()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        await InsertTestDashboardEntry(projectId, jobId, "test-low-runs", runCount: 15, flipCount: 5);
        await InsertTestDashboardEntry(projectId, jobId, "test-high-runs", runCount: 50, flipCount: 10);

        // Act
        var result = await database.GetFlakyTests(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1, "should exclude tests with RunCount <= 20");
        result[0].TestId.Should().Be("test-high-runs");
    }

    [Fact]
    public async Task GetFlakyTests_WithOldTests_ShouldExcludeTestsOlderThan7Days()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        await InsertTestDashboardEntry(projectId, jobId, "recent-test", runCount: 100, flipCount: 15, lastRunDate: DateTime.UtcNow.AddDays(-3));
        await InsertTestDashboardEntry(projectId, jobId, "old-test", runCount: 100, flipCount: 20, lastRunDate: DateTime.UtcNow.AddDays(-10));

        // Act
        var result = await database.GetFlakyTests(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TestId.Should().Be("recent-test");
    }

    [Fact]
    public async Task GetFlakyTests_WithNonExistentJob_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetFlakyTests("non-existent-project", "non-existent-job");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFlakyTestNames_WithFlakyTests_ShouldReturnTestNames()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-1", runCount: 100, flipCount: 15);
        await InsertTestDashboardEntry(projectId, jobId, "flaky-test-2", runCount: 50, flipCount: 10);
        await InsertTestDashboardEntry(projectId, jobId, "stable-test", runCount: 100, flipCount: 5);

        // Act
        var result = await database.GetFlakyTestNames(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain("flaky-test-1");
        result.Should().Contain("flaky-test-2");
        result.Should().NotContain("stable-test");
    }

    [Fact]
    public async Task GetFlakyTestNames_ShouldLimitTo1000Results()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        for (int i = 0; i < 1100; i++)
        {
            await InsertTestDashboardEntry(projectId, jobId, $"flaky-test-{i:D4}", runCount: 100, flipCount: 15);
        }

        // Act
        var result = await database.GetFlakyTestNames(projectId, jobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1000);
    }

    [Fact]
    public async Task GetFlakyTestNames_WithCustomThreshold_ShouldFilterCorrectly()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";

        await InsertTestDashboardEntry(projectId, jobId, "test-low-flip", runCount: 100, flipCount: 15);
        await InsertTestDashboardEntry(projectId, jobId, "test-high-flip", runCount: 100, flipCount: 25);

        // Act
        var result = await database.GetFlakyTestNames(projectId, jobId, flipRateThreshold: 0.2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().Be("test-high-flip");
    }

    [Fact]
    public async Task GetFlakyTestNames_WithNonExistentJob_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetFlakyTestNames("non-existent-project", "non-existent-job");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestRuns_WithExistingTests_ShouldReturnCorrectResults()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfo1 = CreateJobRunInfo(jobId: jobId, projectId: projectId);
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobId, projectId: projectId);
        var jobRunInfo3 = CreateJobRunInfo(jobId: jobId, projectId: projectId);

        var job1 = CreateFullJobInfo(jobId: jobId, jobRunId: jobRunInfo1.JobRunId, projectId: projectId, pipelineId: "pipeline-1", customStatusMessage: "Build 1");
        var job2 = CreateFullJobInfo(jobId: jobId, jobRunId: jobRunInfo2.JobRunId, projectId: projectId, pipelineId: "pipeline-2", customStatusMessage: "Build 2", duration: 1200, totalTests: 12, successTests: 12);
        var job3 = CreateFullJobInfo(jobId: jobId, jobRunId: jobRunInfo3.JobRunId, projectId: projectId, pipelineId: "pipeline-3", customStatusMessage: "Build 3", duration: 900, totalTests: 8, successTests: 8);

        await database.JobInfo.InsertAsync(new[] { job1, job2, job3 });

        var tests1 = new[] { CreateTestRun(testId, TestResult.Success, duration: 5000L, startDateTime: DateTime.UtcNow.AddHours(-3)) };
        var tests2 = new[] { CreateTestRun(testId, TestResult.Failed, duration: 3000L, startDateTime: DateTime.UtcNow.AddHours(-2)) };
        var tests3 = new[] { CreateTestRun(testId, TestResult.Success, duration: 4000L, startDateTime: DateTime.UtcNow.AddHours(-1)) };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);
        await database.TestRuns.InsertBatchAsync(jobRunInfo3, tests3);

        // Act
        var result = await database.GetTestRuns(testId, new[] { jobId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Should be ordered by StartDateTime DESC
        result[0].State.Should().Be("Success");
        result[0].Duration.Should().Be(4000);
        result[0].CustomStatusMessage.Should().Be("Build 3");

        result[1].State.Should().Be("Failed");
        result[1].Duration.Should().Be(3000);
        result[1].CustomStatusMessage.Should().Be("Build 2");

        result[2].State.Should().Be("Success");
        result[2].Duration.Should().Be(5000);
        result[2].CustomStatusMessage.Should().Be("Build 1");
    }

    [Fact]
    public async Task GetTestRuns_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfoMain = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "main");
        var jobRunInfoDevelop = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "develop");

        var jobMain = CreateFullJobInfo(jobId: jobId, jobRunId: jobRunInfoMain.JobRunId, projectId: projectId, pipelineId: "pipeline-1", branchName: "main");
        var jobDevelop = CreateFullJobInfo(jobId: jobId, jobRunId: jobRunInfoDevelop.JobRunId, projectId: projectId, pipelineId: "pipeline-2", branchName: "develop", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5);

        await database.JobInfo.InsertAsync(new[] { jobMain, jobDevelop });

        var testsMain = new[] { CreateTestRun(testId, TestResult.Success, duration: 5000L) };
        var testsDevelop = new[] { CreateTestRun(testId, TestResult.Failed, duration: 3000L) };

        await database.TestRuns.InsertBatchAsync(jobRunInfoMain, testsMain);
        await database.TestRuns.InsertBatchAsync(jobRunInfoDevelop, testsDevelop);

        // Act
        var result = await database.GetTestRuns(testId, new[] { jobId }, branchName: "main");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].BranchName.Should().Be("main");
        result[0].State.Should().Be("Success");
    }

    [Fact]
    public async Task GetTestRuns_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        // Create 100 test runs
        for (int i = 0; i < 100; i++)
        {
            var jobRunInfo = CreateJobRunInfo(jobId: jobId, projectId: projectId);
            var job = CreateFullJobInfo(jobId: jobId, jobRunId: jobRunInfo.JobRunId, projectId: projectId, pipelineId: $"pipeline-{i}", customStatusMessage: $"Build {i}");
            await database.JobInfo.InsertAsync(job);

            var tests = new[] { CreateTestRun(testId, TestResult.Success, duration: (i + 1) * 100L, startDateTime: DateTime.UtcNow.AddHours(-i)) };
            await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);
        }

        // Act - Get first page (default 50 items)
        var page0 = await database.GetTestRuns(testId, new[] { jobId }, page: 0, pageSize: 50);

        // Act - Get second page
        var page1 = await database.GetTestRuns(testId, new[] { jobId }, page: 1, pageSize: 50);

        // Assert
        page0.Should().NotBeNull();
        page0.Should().HaveCount(50);

        page1.Should().NotBeNull();
        page1.Should().HaveCount(50);

        // Verify pages don't overlap
        var page0Ids = page0.Select(r => r.JobRunId).ToHashSet();
        var page1Ids = page1.Select(r => r.JobRunId).ToHashSet();
        page0Ids.Intersect(page1Ids).Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestRuns_WithMultipleJobIds_ShouldReturnFromAllJobs()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId1 = "test-job-1";
        var jobId2 = "test-job-2";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfo1 = CreateJobRunInfo(jobId: jobId1, projectId: projectId);
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobId2, projectId: projectId);

        var job1 = CreateFullJobInfo(jobId: jobId1, jobRunId: jobRunInfo1.JobRunId, projectId: projectId, pipelineId: "pipeline-1");
        var job2 = CreateFullJobInfo(jobId: jobId2, jobRunId: jobRunInfo2.JobRunId, projectId: projectId, pipelineId: "pipeline-2", agentName: "agent-2", duration: 1500, totalTests: 5, successTests: 5);

        await database.JobInfo.InsertAsync(new[] { job1, job2 });

        var tests1 = new[] { CreateTestRun(testId, TestResult.Success, duration: 5000L) };
        var tests2 = new[] { CreateTestRun(testId, TestResult.Failed, duration: 3000L) };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);

        // Act
        var result = await database.GetTestRuns(testId, new[] { jobId1, jobId2 });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.JobId == jobId1 && r.State == "Success");
        result.Should().Contain(r => r.JobId == jobId2 && r.State == "Failed");
    }

    [Fact]
    public async Task GetTestRuns_WithNonExistentTest_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestRuns("non-existent-test", new[] { "non-existent-job" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestRunCount_WithExistingTests_ShouldReturnCorrectCount()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfo1 = CreateJobRunInfo(jobId: jobId, projectId: projectId);
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobId, projectId: projectId);
        var jobRunInfo3 = CreateJobRunInfo(jobId: jobId, projectId: projectId);

        var tests1 = new[] { CreateTestRun(testId, TestResult.Success) };
        var tests2 = new[] { CreateTestRun(testId, TestResult.Failed) };
        var tests3 = new[] { CreateTestRun(testId, TestResult.Success) };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);
        await database.TestRuns.InsertBatchAsync(jobRunInfo3, tests3);

        // Act
        var result = await database.GetTestRunCount(testId, new[] { jobId });

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetTestRunCount_WithBranchFilter_ShouldReturnOnlyMatchingBranch()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId = "test-job";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfoMain1 = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "main");
        var jobRunInfoMain2 = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "main");
        var jobRunInfoDevelop = CreateJobRunInfo(jobId: jobId, projectId: projectId, branchName: "develop");

        var testsMain1 = new[] { CreateTestRun(testId, TestResult.Success) };
        var testsMain2 = new[] { CreateTestRun(testId, TestResult.Success) };
        var testsDevelop = new[] { CreateTestRun(testId, TestResult.Failed) };

        await database.TestRuns.InsertBatchAsync(jobRunInfoMain1, testsMain1);
        await database.TestRuns.InsertBatchAsync(jobRunInfoMain2, testsMain2);
        await database.TestRuns.InsertBatchAsync(jobRunInfoDevelop, testsDevelop);

        // Act
        var result = await database.GetTestRunCount(testId, new[] { jobId }, branchName: "main");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetTestRunCount_WithMultipleJobIds_ShouldCountFromAllJobs()
    {
        // Arrange
        var projectId = $"project-{Guid.NewGuid()}";
        var jobId1 = "test-job-1";
        var jobId2 = "test-job-2";
        var testId = $"test-{Guid.NewGuid()}";

        var jobRunInfo1 = CreateJobRunInfo(jobId: jobId1, projectId: projectId);
        var jobRunInfo2 = CreateJobRunInfo(jobId: jobId1, projectId: projectId);
        var jobRunInfo3 = CreateJobRunInfo(jobId: jobId2, projectId: projectId);

        var tests1 = new[] { CreateTestRun(testId, TestResult.Success) };
        var tests2 = new[] { CreateTestRun(testId, TestResult.Failed) };
        var tests3 = new[] { CreateTestRun(testId, TestResult.Success) };

        await database.TestRuns.InsertBatchAsync(jobRunInfo1, tests1);
        await database.TestRuns.InsertBatchAsync(jobRunInfo2, tests2);
        await database.TestRuns.InsertBatchAsync(jobRunInfo3, tests3);

        // Act
        var result = await database.GetTestRunCount(testId, new[] { jobId1, jobId2 });

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetTestRunCount_WithNonExistentTest_ShouldReturnZero()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestRunCount("non-existent-test", new[] { "non-existent-job" });

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetTestRunCount_WithEmptyJobIds_ShouldReturnZero()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestRunCount("test-id", Array.Empty<string>());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetTestListForCsv_WithBasicTests_ShouldReturnCorrectResults()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Test2", TestResult.Failed, duration: 3000L),
            CreateTestRun("Test3", TestResult.Skipped, duration: 0L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListForCsv(jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Results should have row numbers starting from 1
        result.Should().Contain(t => t.RowNumber == 1);
        result.Should().Contain(t => t.TestId == "Test1" && t.State == "Success" && t.Duration == 5000);
        result.Should().Contain(t => t.TestId == "Test2" && t.State == "Failed" && t.Duration == 3000);
        result.Should().Contain(t => t.TestId == "Test3" && t.State == "Skipped" && t.Duration == 0);
    }

    [Fact]
    public async Task GetTestListForCsv_WithEmptyJobRunIds_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestListForCsv("JobId", Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestListForCsv_WithNonExistentJobRunId_ShouldReturnEmptyArray()
    {
        // Arrange - no data

        // Act
        var result = await database.GetTestListForCsv("JobId", new[] { "non-existent-jobrun" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTestListForCsv_ShouldReturnRowNumbers()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test1", TestResult.Success, duration: 5000L),
            CreateTestRun("Test2", TestResult.Failed, duration: 3000L),
            CreateTestRun("Test3", TestResult.Skipped, duration: 0L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListForCsv(jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Check that row numbers are sequential and start from 1
        result.Select(r => r.RowNumber).Should().Contain(new long[] { 1, 2, 3 });
    }

    [Fact]
    public async Task GetTestListForCsv_WithSpecialCharactersInTestId_ShouldHandleCorrectly()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new[]
        {
            CreateTestRun("Test's with 'quotes'", TestResult.Success, duration: 5000L),
            CreateTestRun("Test with % wildcard", TestResult.Failed, duration: 3000L),
            CreateTestRun("Test_with_underscores", TestResult.Success, duration: 1000L)
        };

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListForCsv(jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.TestId == "Test's with 'quotes'");
        result.Should().Contain(t => t.TestId == "Test with % wildcard");
        result.Should().Contain(t => t.TestId == "Test_with_underscores");
    }

    [Fact]
    public async Task GetTestListForCsv_WithLargeDataset_ShouldReturnAllRows()
    {
        // Arrange
        var jobRunInfo = CreateJobRunInfo();
        var tests = new List<TestRun>();
        
        for (int i = 0; i < 500; i++)
        {
            tests.Add(CreateTestRun($"Test{i:D3}", TestResult.Success, duration: (i + 1) * 100L));
        }

        await database.TestRuns.InsertBatchAsync(jobRunInfo, tests);

        // Act
        var result = await database.GetTestListForCsv(jobRunInfo.JobId, new[] { jobRunInfo.JobRunId });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(500);
        result.All(r => r.RowNumber > 0).Should().BeTrue("all rows should have positive row numbers");
    }
}

