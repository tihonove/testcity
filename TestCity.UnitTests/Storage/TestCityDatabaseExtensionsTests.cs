using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;
using static TestCity.UnitTests.Utils.TestDataBuilders;

namespace TestCity.UnitTests.Storage;

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
    public async Task GetFailedTestOutput_WithFailedTest_ShouldReturnCorrectOutput()
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
        var jobRunInfo = CreateJobRunInfo(jobId: "test-job-success");
        var testId = $"test-success-{Guid.NewGuid()}";
        var successfulTest = CreateTestRun(
            testId: testId,
            systemOutput: "System output for successful test");

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
        // Arrange - no setup needed

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
}

