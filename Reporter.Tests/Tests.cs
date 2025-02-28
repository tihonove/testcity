using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using Kontur.TestAnalytics.Core.Clickhouse;
using Kontur.TestAnalytics.Reporter.Cli;
using Kontur.TestAnalytics.Reporter.Client;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class Tests
{
    [Test]
    public async Task Test01()
    {
        await using var connection = CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO TestRuns (TestId) VALUES ('123');";
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Test013()
    {
        await using var connection = CreateConnection();
        var files = Directory.GetFileSystemEntries(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TestData"), "*.csv");
        var dateTime = DateTime.Now;
        foreach (var file in files)
        {
            var buildId = file.Substring(file.IndexOf("Wolfs_Unit_tests_")).Replace("Wolfs_Unit_tests_", string.Empty)
                .Replace("-tests.csv", string.Empty);
            Console.WriteLine(buildId);
            var lines = TestRunsReader.ReadFromTeamcityTestReport(file);
            dateTime = dateTime.AddDays(-1);
            await TestRunsUploader.UploadAsync(
                new JobRunInfo
                {
                    JobId = "Forms_UnitTests",
                    PipelineId = buildId + "123",
                    JobRunId = buildId + "1",
                    BranchName = "master",
                    AgentName = "KE-FRM-AGENT-01",
                    AgentOSName = "Windows",
                    JobUrl = "https://kontur.ru",
                }, lines);
            await TestRunsUploader.UploadAsync(
                new JobRunInfo
                {
                    JobId = "Forms_UnitTests",
                    PipelineId = buildId + "123",
                    JobRunId = buildId + "2",
                    BranchName = "tihonove/branch-1",
                    AgentName = "KE-FRM-AGENT-01",
                    AgentOSName = "Windows",
                    JobUrl = "https://kontur.ru",
                }, lines);
            await TestRunsUploader.UploadAsync(
                new JobRunInfo
                {
                    JobId = "Forms_UnitTests",
                    PipelineId = buildId + "123",
                    JobRunId = buildId + "3",
                    BranchName = "tihonove/branch-2",
                    AgentName = "KE-FRM-AGENT-01",
                    AgentOSName = "Linux",
                    JobUrl = "https://kontur.ru",
                }, lines);
        }
    }

    private static readonly string[] Columns = new[]
        {
            "JobId",
            "JobRunId",
            "BranchName",
            "TestId",
            "State",
            "Duration",
            "StartDateTime",
            "AgentName",
            "AgentOSName",
        };

    [Test]
    public async Task Test012()
    {
        await using var connection = CreateConnection();

        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "TestRuns",
            BatchSize = 100,
        };
        var values = new[] { new object[] { "1", "1", "1", "1", 1, 1, DateTime.Now.ToUniversalTime(), "1" } };
        await bulkCopyInterface.WriteToServerAsync(values, Columns);
        Console.WriteLine(bulkCopyInterface.RowsWritten);
    }

    [Test]
    public async Task Test03()
    {
        var lines = await TestRunsReader.ReadFromTeamcityTestReport(
            "/home/tihonove/Downloads/Wolfs_Unit_tests_11509-tests.csv").ToArrayAsync();
    }

    [Test]
    public async Task Test04()
    {
        var lines = TestRunsReader.ReadFromTeamcityTestReport(
            "/home/tihonove/Downloads/Wolfs_Unit_tests_11509-tests.csv");
        var runInfo = new JobRunInfo
        {
            JobId = "Forms_UnitTests",
            JobRunId = "32028281",
            PipelineId = "123",
            BranchName = "master",
            AgentName = "AGENT-1",
            AgentOSName = "Windows",
            JobUrl = "https://kontur.ru",
        };
        await TestRunsUploader.UploadAsync(runInfo, lines);
    }

    [Ignore("для ручного запуска")]
    [Test]
    public async Task RecreateTable()
    {
        await using var connection = CreateConnection();
        const string dropTableScript = "DROP TABLE IF EXISTS TestRuns";

        const string createTableScript = @"
            create table TestRuns
            (
                JobId String,
                JobRunId String,
                BranchName String,
                TestId String,
                State Enum8('Success' = 1, 'Failed' = 2, 'Skipped' = 3),
                Duration Decimal64(0),
                StartDateTime DateTime,
                AgentName String,
                AgentOSName String,
                JobUrl String
            )
            engine = MergeTree()
            ORDER BY (JobId, JobRunId, BranchName, TestId);
        ";
        await connection.ExecuteStatementAsync(dropTableScript);
        await connection.ExecuteStatementAsync(createTableScript);
    }

    private static ClickHouseConnection CreateConnection()
    {
        return ConnectionFactory.CreateConnection();
    }
}
