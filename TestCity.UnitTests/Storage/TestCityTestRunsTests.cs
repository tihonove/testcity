using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Storage;

[Collection("Global")]
public class TestCityTestRunsTests : IAsyncLifetime
{
    public TestCityTestRunsTests(ITestOutputHelper output)
    {
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
    public async Task InsertBatchAsync_WithJobRunInfoAndTestRuns_ShouldInsertCorrectly()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;
        
        var jobRunInfo = CreateUniqueJobRunInfo();
        var testRunsList = new List<TestRun>
        {
            CreateTestRun("Test1", TestResult.Success),
            CreateTestRun("Test2", TestResult.Failed, "Error message", "Error output", "System output"),
            CreateTestRun("Test3", TestResult.Skipped)
        };

        // Act
        await testRuns.InsertBatchAsync(jobRunInfo, testRunsList);

        // Assert
        var retrievedData = new List<(JobRunInfo, TestRun)>();
        await foreach (var item in testRuns.GetAllAsync())
        {
            if (item.Item1.JobRunId == jobRunInfo.JobRunId)
            {
                retrievedData.Add(item);
            }
        }

        retrievedData.Should().HaveCount(3);
        
        var test1 = retrievedData.SingleOrDefault(x => x.Item2.TestId == "Test1");
        test1.Should().NotBeNull();
        test1.Item1.JobId.Should().Be(jobRunInfo.JobId);
        test1.Item1.ProjectId.Should().Be(jobRunInfo.ProjectId);
        test1.Item2.TestResult.Should().Be(TestResult.Success);
        test1.Item2.JUnitFailureMessage.Should().BeNullOrEmpty();

        var test2 = retrievedData.SingleOrDefault(x => x.Item2.TestId == "Test2");
        test2.Should().NotBeNull();
        test2.Item2.TestResult.Should().Be(TestResult.Failed);
        test2.Item2.JUnitFailureMessage.Should().Be("Error message");
        test2.Item2.JUnitFailureOutput.Should().Be("Error output");
        test2.Item2.JUnitSystemOutput.Should().Be("System output");

        var test3 = retrievedData.SingleOrDefault(x => x.Item2.TestId == "Test3");
        test3.Should().NotBeNull();
        test3.Item2.TestResult.Should().Be(TestResult.Skipped);
    }

    [Fact]
    public async Task InsertBatchAsync_WithAsyncEnumerableJobRunInfoAndTestRuns_ShouldInsertCorrectly()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;
        
        var jobRunInfo = CreateUniqueJobRunInfo();
        var testRunsList = new List<TestRun>
        {
            CreateTestRun("AsyncTest1", TestResult.Success),
            CreateTestRun("AsyncTest2", TestResult.Failed)
        };

        // Act
        await testRuns.InsertBatchAsync(jobRunInfo, testRunsList.ToAsyncEnumerable());

        // Assert
        var retrievedData = new List<(JobRunInfo, TestRun)>();
        await foreach (var item in testRuns.GetAllAsync())
        {
            if (item.Item1.JobRunId == jobRunInfo.JobRunId)
            {
                retrievedData.Add(item);
            }
        }

        retrievedData.Should().HaveCount(2);
        retrievedData.Should().AllSatisfy(x => x.Item1.JobRunId.Should().Be(jobRunInfo.JobRunId));
    }

    [Fact]
    public async Task InsertBatchAsync_WithTupleAsyncEnumerable_ShouldInsertCorrectly()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;
        
        var jobRunInfo1 = CreateUniqueJobRunInfo();
        var jobRunInfo2 = CreateUniqueJobRunInfo();
        
        var tupleList = new List<(JobRunInfo, TestRun)>
        {
            (jobRunInfo1, CreateTestRun("TupleTest1", TestResult.Success)),
            (jobRunInfo2, CreateTestRun("TupleTest2", TestResult.Failed))
        };

        // Act
        await testRuns.InsertBatchAsync(tupleList.ToAsyncEnumerable());

        // Assert
        var retrievedData = new List<(JobRunInfo, TestRun)>();
        await foreach (var item in testRuns.GetAllAsync())
        {
            if (item.Item1.JobRunId == jobRunInfo1.JobRunId || item.Item1.JobRunId == jobRunInfo2.JobRunId)
            {
                retrievedData.Add(item);
            }
        }

        retrievedData.Should().HaveCount(2);
        
        var job1Results = retrievedData.Where(x => x.Item1.JobRunId == jobRunInfo1.JobRunId).ToList();
        job1Results.Should().HaveCount(1);
        job1Results[0].Item2.TestId.Should().Be("TupleTest1");
        job1Results[0].Item2.TestResult.Should().Be(TestResult.Success);

        var job2Results = retrievedData.Where(x => x.Item1.JobRunId == jobRunInfo2.JobRunId).ToList();
        job2Results.Should().HaveCount(1);
        job2Results[0].Item2.TestId.Should().Be("TupleTest2");
        job2Results[0].Item2.TestResult.Should().Be(TestResult.Failed);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTestRuns()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;
        
        var jobRunInfo = CreateUniqueJobRunInfo();
        var testRunsList = new List<TestRun>
        {
            CreateTestRun("GetAllTest1", TestResult.Success),
            CreateTestRun("GetAllTest2", TestResult.Failed)
        };

        await testRuns.InsertBatchAsync(jobRunInfo, testRunsList);

        // Act
        var retrievedData = new List<(JobRunInfo, TestRun)>();
        await foreach (var item in testRuns.GetAllAsync())
        {
            if (item.Item1.JobRunId == jobRunInfo.JobRunId)
            {
                retrievedData.Add(item);
            }
        }

        // Assert
        retrievedData.Should().HaveCount(2);
        
        retrievedData.Should().AllSatisfy(x =>
        {
            x.Item1.JobId.Should().Be(jobRunInfo.JobId);
            x.Item1.ProjectId.Should().Be(jobRunInfo.ProjectId);
            x.Item1.BranchName.Should().Be(jobRunInfo.BranchName);
            x.Item1.AgentName.Should().Be(jobRunInfo.AgentName);
            x.Item1.AgentOSName.Should().Be(jobRunInfo.AgentOSName);
            x.Item1.JobUrl.Should().Be(jobRunInfo.JobUrl);
        });

        var test1 = retrievedData.SingleOrDefault(x => x.Item2.TestId == "GetAllTest1");
        test1.Should().NotBeNull();
        test1.Item2.TestResult.Should().Be(TestResult.Success);

        var test2 = retrievedData.SingleOrDefault(x => x.Item2.TestId == "GetAllTest2");
        test2.Should().NotBeNull();
        test2.Item2.TestResult.Should().Be(TestResult.Failed);
    }

    [Fact]
    public async Task InsertBatchAsync_WithLargeDataset_ShouldHandleBatching()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;
        
        var jobRunInfo = CreateUniqueJobRunInfo();
        var testRunsList = new List<TestRun>();
        
        for (int i = 0; i < 1500; i++)
        {
            testRunsList.Add(CreateTestRun($"BatchTest{i}", TestResult.Success));
        }

        // Act
        await testRuns.InsertBatchAsync(jobRunInfo, testRunsList);

        // Assert
        var retrievedData = new List<(JobRunInfo, TestRun)>();
        await foreach (var item in testRuns.GetAllAsync())
        {
            if (item.Item1.JobRunId == jobRunInfo.JobRunId)
            {
                retrievedData.Add(item);
            }
        }

        retrievedData.Should().HaveCount(1500);
        retrievedData.Should().AllSatisfy(x => x.Item2.TestResult.Should().Be(TestResult.Success));
    }

    [Fact]
    public async Task InsertBatchAsync_WithEmptyList_ShouldNotThrow()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;
        
        var jobRunInfo = CreateUniqueJobRunInfo();
        var emptyTestRunsList = new List<TestRun>();

        // Act & Assert
        var act = () => testRuns.InsertBatchAsync(jobRunInfo, emptyTestRunsList);
        await act.Should().NotThrowAsync();
    }

    private static JobRunInfo CreateUniqueJobRunInfo()
    {
        var uniqueId = Guid.NewGuid().ToString();
        return new JobRunInfo
        {
            JobId = $"test-job-{uniqueId}",
            JobRunId = $"test-job-run-{uniqueId}",
            ProjectId = "test-project-id",
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "Linux",
            JobUrl = $"https://example.com/job/{uniqueId}",
            PipelineId = $"pipeline-{uniqueId}"
        };
    }

    private static TestRun CreateTestRun(string testId, TestResult result, string? failureMessage = null, string? failureOutput = null, string? systemOutput = null)
    {
        return new TestRun
        {
            TestId = testId,
            TestResult = result,
            Duration = 1000,
            StartDateTime = DateTime.UtcNow,
            JUnitFailureMessage = result != TestResult.Success ? failureMessage : null,
            JUnitFailureOutput = result != TestResult.Success ? failureOutput : null,
            JUnitSystemOutput = result != TestResult.Success ? systemOutput : null
        };
    }
}
