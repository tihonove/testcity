using FluentAssertions;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Storage;

[Collection("Global")]
public class TestCityFlakyTestsTests : IAsyncLifetime
{
    public TestCityFlakyTestsTests(ITestOutputHelper output)
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
    public async Task DetectFlakyTests_WithFiftyPercentFlipRate_ShouldIdentifyFlakyTest()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testRuns = new TestCityDatabase(connectionFactory).TestRuns;

        var flakyTestId = "FlakyTest.ShouldBeFlaky" + Guid.NewGuid().ToString("N");
        var testRunsData = new List<(JobRunInfo, TestRun)>();
        var baseTime = DateTime.Today.AddHours(12);

        testRunsData.Add((
            CreateJobRunInfo(1),
            CreateTestRun(flakyTestId, TestResult.Success, 1500, baseTime.AddMinutes(0))
        ));

        testRunsData.Add((
            CreateJobRunInfo(2),
            CreateTestRun(flakyTestId, TestResult.Success, 1800, baseTime.AddMinutes(10))
        ));

        testRunsData.Add((
            CreateJobRunInfo(3),
            CreateTestRun(flakyTestId, TestResult.Success, 2200, baseTime.AddMinutes(20))
        ));

        testRunsData.Add((
            CreateJobRunInfo(4),
            CreateTestRun(flakyTestId, TestResult.Failed, 2000, baseTime.AddMinutes(30), "Test failed on assertion")
        ));

        testRunsData.Add((
            CreateJobRunInfo(5),
            CreateTestRun(flakyTestId, TestResult.Failed, 1200, baseTime.AddMinutes(40), "Network timeout error")
        ));

        testRunsData.Add((
            CreateJobRunInfo(6),
            CreateTestRun(flakyTestId, TestResult.Failed, 1600, baseTime.AddMinutes(50), "Database connection lost")
        ));

        testRunsData.Add((
            CreateJobRunInfo(7),
            CreateTestRun(flakyTestId, TestResult.Success, 1900, baseTime.AddMinutes(60))
        ));

        testRunsData.Add((
            CreateJobRunInfo(8),
            CreateTestRun(flakyTestId, TestResult.Failed, 1100, baseTime.AddMinutes(70), "Memory allocation failed")
        ));

        testRunsData.Add((
            CreateJobRunInfo(9),
            CreateTestRun(flakyTestId, TestResult.Failed, 1700, baseTime.AddMinutes(80), "Race condition detected")
        ));

        testRunsData.Add((
            CreateJobRunInfo(10),
            CreateTestRun(flakyTestId, TestResult.Success, 2100, baseTime.AddMinutes(90))
        ));

        // Act - insert all test runs
        await testRuns.InsertBatchAsync(testRunsData.ToAsyncEnumerable());

        // Assert - retrieve all data and verify flip rate
        var retrievedData = new List<(JobRunInfo, TestRun)>();
        await foreach (var item in testRuns.GetAllAsync())
        {
            if (item.Item2.TestId == flakyTestId)
            {
                retrievedData.Add(item);
            }
        }

        retrievedData.Should().HaveCount(10);

        var successfulRuns = retrievedData.Count(x => x.Item2.TestResult == TestResult.Success);
        var failedRuns = retrievedData.Count(x => x.Item2.TestResult == TestResult.Failed);

        successfulRuns.Should().Be(5);
        failedRuns.Should().Be(5);

        // Calculate flip rate (percentage of failed runs)
        var flipRate = (double)failedRuns / retrievedData.Count;
        flipRate.Should().Be(0.5); // 50% flip rate
 
        // Verify that all runs belong to the same test
        retrievedData.Should().AllSatisfy(x => x.Item2.TestId.Should().Be(flakyTestId));

        // Verify that failed runs have error messages
        var failedTestRuns = retrievedData.Where(x => x.Item2.TestResult == TestResult.Failed);
        failedTestRuns.Should().AllSatisfy(x => x.Item2.JUnitFailureMessage.Should().NotBeNullOrEmpty());
    }

    private static JobRunInfo CreateJobRunInfo(int runNumber)
    {
        return new JobRunInfo
        {
            JobId = "flaky-test-job•x",
            JobRunId = $"flaky-test-job-run-{runNumber}",
            ProjectId = "flaky-test-project-id",
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "Linux",
            JobUrl = $"https://example.com/job/{runNumber}",
            PipelineId = $"pipeline-{runNumber}"
        };
    }

    private static TestRun CreateTestRun(string testId, TestResult result, int duration, DateTime startTime, string? failureMessage = null)
    {
        return new TestRun
        {
            TestId = testId,
            TestResult = result,
            Duration = duration,
            StartDateTime = startTime,
            JUnitFailureMessage = result != TestResult.Success ? failureMessage : null,
            JUnitFailureOutput = result != TestResult.Success ? $"Stack trace for {testId}" : null,
            JUnitSystemOutput = result != TestResult.Success ? $"System output for {testId}" : null
        };
    }
}
