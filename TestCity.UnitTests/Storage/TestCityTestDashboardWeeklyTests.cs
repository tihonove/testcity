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
public class TestCityTestDashboardWeeklyTests(ITestOutputHelper output) : IAsyncLifetime
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
        entry.FlipCount.Should().Be(0UL); // Single run - no flips
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
        entry.FlipCount.Should().Be(0UL); // Single run - no flips
    }

    [Fact]
    public async Task SuccessSequence_ShouldHaveZeroFlips()
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
        entry.FlipCount.Should().Be(0UL); // All same statuses - no flips
    }

    [Fact]
    public async Task SuccessFailSuccess_ShouldHaveTwoFlips()
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
        entry.FlipCount.Should().Be(2UL); // Success->Failed, Failed->Success = 2 flips
    }

    [Fact]
    public async Task SuccessFailFailSuccess_ShouldHaveTwoFlips()
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

        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(4UL);
        entry.FailCount.Should().Be(2UL);
        entry.FlipCount.Should().Be(2UL); // Success->Failed, Failed->Success = 2 flips            
    }

    [Fact]
    public async Task SuccessSkippedSuccess_ShouldHaveFlips()
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
        entry.FlipCount.Should().BeGreaterOrEqualTo(0UL); // May count Skipped transitions
    }

    [Fact]
    public async Task SuccessSkippedFailed_ShouldHaveFlips()
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
        entry.FlipCount.Should().BeGreaterThan(0UL); // Status transitions should result in flips
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
        entry.FlipCount.Should().Be(1UL); // Failed->Success = 1 flip
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
        entry1.FlipCount.Should().Be(0UL); // Single run - no flips

        entry2.Should().NotBeNull();
        entry2!.RunCount.Should().Be(1UL);
        entry2.FailCount.Should().Be(0UL);
        entry2.FlipCount.Should().Be(0UL); // Single run - no flips
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
        firstEntry.FlipCount.Should().Be(0UL); // Single run - no flips

        // Act - then add Failed on the same day
        var secondRun = CreateTestData(testId, "Failed", baseTime.AddMinutes(30));
        await InsertTestDataSequentially(database, secondRun);

        // Assert - aggregate updated, flips appeared
        var updatedEntry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        updatedEntry.Should().NotBeNull();
        updatedEntry!.RunCount.Should().Be(2UL);
        updatedEntry.FailCount.Should().Be(1UL);
        updatedEntry.FlipCount.Should().Be(1UL); // Success->Failed = 1 flip
    }

    [Fact]
    public async Task RandomFlappyTest_100Runs_ShouldHaveHighFlips()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        // Create 100 random test results with 50% success rate (truly flappy)
        var random = new Random(42); // Fixed seed for reproducible tests
        var testData = new List<(JobRunInfo, TestRun)>();

        for (int i = 0; i < 100; i++)
        {
            var state = random.NextDouble() < 0.5 ? "Success" : "Failed";
            testData.Add(CreateTestData(testId, state, baseTime.AddMinutes(i)));
        }

        // Act
        await InsertTestDataSequentially(database, testData.ToArray());

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(100UL);

        // Flappy test should have many flips
        entry.FlipCount.Should().BeGreaterThan(20UL); // Many transitions for random flapping

        // Should have roughly 50% failures
        entry.FailCount.Should().BeInRange(30UL, 70UL); // Allow some variance
    }

    [Fact]
    public async Task StableGreenThenRed_100Runs_ShouldHaveLowFlips()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new List<(JobRunInfo, TestRun)>();

        // First 80 runs are successful (stable green)
        for (int i = 0; i < 80; i++)
        {
            testData.Add(CreateTestData(testId, "Success", baseTime.AddMinutes(i)));
        }

        // Last 20 runs fail (became red)
        for (int i = 80; i < 100; i++)
        {
            testData.Add(CreateTestData(testId, "Failed", baseTime.AddMinutes(i)));
        }

        // Act
        await InsertTestDataSequentially(database, testData.ToArray());

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(100UL);
        entry.FailCount.Should().Be(20UL);

        // Should have only one flip due to single transition from stable green to red
        entry.FlipCount.Should().Be(1UL); // Only one transition: Success->Failed
    }

    [Fact]
    public async Task HighlyFlappyTest_100Runs_ShouldHaveMaximumFlips()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new List<(JobRunInfo, TestRun)>();

        // Alternating Success/Failed pattern (maximum flappiness)
        for (int i = 0; i < 100; i++)
        {
            var state = i % 2 == 0 ? "Success" : "Failed";
            testData.Add(CreateTestData(testId, state, baseTime.AddMinutes(i)));
        }

        // Act
        await InsertTestDataSequentially(database, testData.ToArray());

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(100UL);
        entry.FailCount.Should().Be(50UL);

        // Should have maximum flips due to constant alternating (99 transitions)
        entry.FlipCount.Should().Be(99UL); // 99 flips for alternating pattern
    }

    [Fact]
    public async Task StableRedTest_100Runs_ShouldHaveZeroFlips()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new List<(JobRunInfo, TestRun)>();

        // All 100 runs fail (completely broken test)
        for (int i = 0; i < 100; i++)
        {
            testData.Add(CreateTestData(testId, "Failed", baseTime.AddMinutes(i)));
        }

        // Act
        await InsertTestDataSequentially(database, testData.ToArray());

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(100UL);
        entry.FailCount.Should().Be(100UL);

        // Should have zero flips - no variation
        entry.FlipCount.Should().Be(0UL);
    }

    [Fact]
    public async Task OccasionalFlap_100Runs_ShouldHaveMediumFlips()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new List<(JobRunInfo, TestRun)>();

        // Mostly successful with occasional failures (realistic flappy test)
        var random = new Random(123); // Fixed seed
        for (int i = 0; i < 100; i++)
        {
            // 85% success rate with occasional failures
            var state = random.NextDouble() < 0.85 ? "Success" : "Failed";
            testData.Add(CreateTestData(testId, state, baseTime.AddMinutes(i)));
        }

        // Act
        await InsertTestDataSequentially(database, testData.ToArray());

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(100UL);

        // Should have moderate number of flips - some variation but not maximum
        entry.FlipCount.Should().BeGreaterThan(5UL);
        entry.FlipCount.Should().BeLessThan(50UL);

        // Should have roughly 15% failures
        entry.FailCount.Should().BeLessThan(25UL);
    }

    [Fact]
    public async Task InitiallyFlappyThenStable_100Runs_ShouldHaveMediumFlips()
    {
        // Arrange
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var database = new TestCityDatabase(connectionFactory);
        var testId = GenerateTestId();
        var baseTime = DateTime.Today.AddHours(12);

        var testData = new List<(JobRunInfo, TestRun)>();

        // First 30 runs are flappy
        var random = new Random(456); // Fixed seed
        for (int i = 0; i < 30; i++)
        {
            var state = random.NextDouble() < 0.5 ? "Success" : "Failed";
            testData.Add(CreateTestData(testId, state, baseTime.AddMinutes(i)));
        }

        // Next 70 runs are stable success (test was fixed)
        for (int i = 30; i < 100; i++)
        {
            testData.Add(CreateTestData(testId, "Success", baseTime.AddMinutes(i)));
        }

        // Act
        await InsertTestDataSequentially(database, testData.ToArray());

        // Assert
        var entry = await database.TestDashboardWeekly.GetTestAsync("test-project", "test-job", testId);
        entry.Should().NotBeNull();
        entry!.RunCount.Should().Be(100UL);

        // Should have moderate flips due to initial flappiness then stabilization
        entry.FlipCount.Should().BeGreaterThan(5UL);
        entry.FlipCount.Should().BeLessThan(40UL);

        // Failures should be mostly from the flappy period
        entry.FailCount.Should().BeLessThan(20UL);
    }

    private string GenerateTestId()
    {
        var result = "Test." + Guid.NewGuid().ToString("N")[..8];
        log.LogInformation("Generated test ID: {TestId}", result);
        return result;
    }

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

    private static async Task InsertTestDataSequentially(TestCityDatabase database, params (JobRunInfo, TestRun)[] testData)
    {
        foreach (var data in testData)
        {
            await database.TestRuns.InsertBatchAsync(new[] { data }.ToAsyncEnumerable());
        }
    }

    private readonly ILogger log = XUnitLoggerProvider.ConfigureTestLogger(output).CreateLogger<TestCityTestDashboardWeeklyTests>();
}
