using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using Kontur.TestAnalytics.Core.Clickhouse;
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
            ColumnNames = Columns,
        };
        await bulkCopyInterface.InitAsync();
        var values = new[] { new object[] { "1", "1", "1", "1", 1, 1L, DateTime.Now.ToUniversalTime(), "1", "1" } };
        await bulkCopyInterface.WriteToServerAsync(values);
        Console.WriteLine(bulkCopyInterface.RowsWritten);
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
