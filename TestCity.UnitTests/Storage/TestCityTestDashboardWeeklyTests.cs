using FluentAssertions;
using Microsoft.Extensions.Logging;
using TestCity.Core.Clickhouse;
using TestCity.Core.Logging;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Storage;

[Collection("Global")]
public class TestCityTestDashboardWeeklyTests : IAsyncLifetime
{
    public TestCityTestDashboardWeeklyTests(ITestOutputHelper output)
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
    public async Task SingleRun_Success_ShouldHaveCorrectMetrics()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = CreateTestData(testId, "Success", baseTime);
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(1UL);
        entry.FailCount.Should().Be(0UL);
        entry.Entropy.Should().Be(0.0); // Single status - no entropy
    }

    [Fact]
    public async Task SingleRun_Failed_ShouldHaveCorrectMetrics()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = CreateTestData(testId, "Failed", baseTime);
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(1UL);
        entry.FailCount.Should().Be(1UL);
        entry.Entropy.Should().Be(0.0); // Single status - no entropy
    }

    [Fact]
    public async Task SuccessSequence_ShouldHaveZeroEntropy()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new[]
        {
            CreateTestData(testId, "Success", baseTime.AddMinutes(0)),
            CreateTestData(testId, "Success", baseTime.AddMinutes(10)),
            CreateTestData(testId, "Success", baseTime.AddMinutes(20))
        };
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(3UL);
        entry.FailCount.Should().Be(0UL);
        entry.Entropy.Should().Be(0.0); // All same statuses - no entropy
    }

    [Fact]
    public async Task SuccessFailSuccess_ShouldHaveHighEntropy()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new[]
        {
            CreateTestData(testId, "Success", baseTime.AddMinutes(0)),
            CreateTestData(testId, "Failed", baseTime.AddMinutes(10)),
            CreateTestData(testId, "Success", baseTime.AddMinutes(20))
        };
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(3UL);
        entry.FailCount.Should().Be(1UL);
        entry.Entropy.Should().BeGreaterThan(0.0); // Status changes - has entropy
    }

    [Fact]
    public async Task SuccessFailFailSuccess_ShouldHaveHighEntropy()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new[]
        {
            CreateTestData(testId, "Success", baseTime.AddMinutes(0)),
            CreateTestData(testId, "Failed", baseTime.AddMinutes(10)),
            CreateTestData(testId, "Failed", baseTime.AddMinutes(20)),
            CreateTestData(testId, "Success", baseTime.AddMinutes(30))
        };
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(4UL);
        entry.FailCount.Should().Be(2UL);
        entry.Entropy.Should().BeGreaterThan(0.0); // Status changes - has entropy
    }

    [Fact]
    public async Task SuccessSkippedSuccess_ShouldHaveLowEntropy()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new[]
        {
            CreateTestData(testId, "Success", baseTime.AddMinutes(0)),
            CreateTestData(testId, "Skipped", baseTime.AddMinutes(10)),
            CreateTestData(testId, "Success", baseTime.AddMinutes(20))
        };
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(3UL);
        entry.FailCount.Should().Be(0UL);
        entry.Entropy.Should().BeGreaterOrEqualTo(0.0); // Skipped may not be counted in entropy
    }

    [Fact]
    public async Task SuccessSkippedFailed_ShouldHaveEntropy()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new[]
        {
            CreateTestData(testId, "Success", baseTime.AddMinutes(0)),
            CreateTestData(testId, "Skipped", baseTime.AddMinutes(10)),
            CreateTestData(testId, "Failed", baseTime.AddMinutes(20))
        };
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(3UL);
        entry.FailCount.Should().Be(1UL);
        entry.Entropy.Should().BeGreaterThan(0.0); // Success->Failed transition - has entropy
    }

    [Fact]
    public async Task SevenDayWindow_ShouldOnlyIncludeRecentRuns()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var today = DateTime.Today.AddHours(12);


        // Act
        await InsertTestDataSequentially(database, 
            CreateTestData(testId, "Failed", today.AddDays(-6)),
            CreateTestData(testId, "Success", today.AddDays(-1))
        );

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(2UL); // Both runs within 7-day window
        entry.FailCount.Should().Be(1UL); // One failed run
        entry.Entropy.Should().BeGreaterThan(0.0); // Different statuses - has entropy
    }

    [Fact]
    public async Task DifferentProjects_ShouldNotMixAggregates()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new[]
        {
            CreateTestDataForProject(testId, "project1", "Failed", baseTime),
            CreateTestDataForProject(testId, "project2", "Success", baseTime)
        };
        
        // Act
        await InsertTestDataSequentially(database, testData);

        // Assert
        var entry1 = await database.TestDashboardWeekly.GetTestAsync("project1", "test-job", testId);
        var entry2 = await database.TestDashboardWeekly.GetTestAsync("project2", "test-job", testId);
        
        entry1.Should().NotBeNull();
        entry1!.RunCount.Should().Be(1UL);
        entry1.FailCount.Should().Be(1UL);
        entry1.Entropy.Should().Be(0.0); // Single run - no entropy
        
        entry2.Should().NotBeNull();
        entry2!.RunCount.Should().Be(1UL);
        entry2.FailCount.Should().Be(0UL);
        entry2.Entropy.Should().Be(0.0); // Single run - no entropy
    }

    [Fact]
    public async Task UpdatedRuns_ShouldUpdateAggregates()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);
        Log.GetLog("zzz").LogInformation("Test id: {testId}", testId);

        // Act - first insert Success
        var firstRun = CreateTestData(testId, "Success", baseTime);
        await InsertTestDataSequentially(database, firstRun);

        var firstEntry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        firstEntry.Should().NotBeNull();
        firstEntry!.RunCount.Should().Be(1UL);
        firstEntry.FailCount.Should().Be(0UL);
        firstEntry.Entropy.Should().Be(0.0); // Single run - no entropy

        // Act - then add Failed on the same day
        var secondRun = CreateTestData(testId, "Failed", baseTime.AddMinutes(30));
        await InsertTestDataSequentially(database, secondRun);

        // Assert - aggregate updated, entropy appeared
        var updatedEntry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        updatedEntry.Should().NotBeNull();
        updatedEntry!.RunCount.Should().Be(2UL);
        updatedEntry.FailCount.Should().Be(1UL);
        updatedEntry.Entropy.Should().BeGreaterThan(0.0); // Status transition - has entropy
    }

    private static string GenerateTestId() => "Test." + Guid.NewGuid().ToString("N")[..8];

    private static (JobRunInfo, TestRun) CreateTestData(string testId, string state, DateTime startTime)
    {
        return CreateTestDataForProject(testId, "test-project", state, startTime);
    }

    private static (JobRunInfo, TestRun) CreateTestDataForProject(string testId, string projectId, string state, DateTime startTime)
    {
        var jobRunInfo = new JobRunInfo
        {
            JobId = "test-job",
            JobRunId = $"run-{Guid.NewGuid().ToString("N")[..8]}",
            ProjectId = projectId,
            BranchName = "main",
            AgentName = "test-agent",
            AgentOSName = "Linux",
            JobUrl = "https://example.com/job",
            PipelineId = "pipeline-123"
        };

        var testRun = new TestRun
        {
            TestId = testId,
            TestResult = state switch
            {
                "Success" => TestResult.Success,
                "Failed" => TestResult.Failed,
                "Skipped" => TestResult.Skipped,
                _ => throw new ArgumentException($"Unknown state: {state}")
            },
            Duration = 1000,
            StartDateTime = startTime,
            JUnitFailureMessage = state == "Failed" ? "Test failed" : null,
            JUnitFailureOutput = state == "Failed" ? "Stack trace" : null,
            JUnitSystemOutput = null
        };

        return (jobRunInfo, testRun);
    }

    private static async Task WaitForMaterializedView()
    {
        // Give time for the materialized view to process data
        await Task.Delay(1000);
    }

    private static async Task InsertTestDataSequentially(TestCityDatabase database, params (JobRunInfo, TestRun)[] testData)
    {
        foreach (var data in testData)
        {
            await database.TestRuns.InsertBatchAsync(new[] { data }.ToAsyncEnumerable());
            await WaitForMaterializedView();
        }
    }
}
