using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;
using ClickHouse.Client.Copy;
using Kontur.TestAnalytics.Reporter.Cli;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class Class1
{
    [Test]
    public async Task Test01()
    {
        await using var connection =
            new ClickHouseConnection("Host=172.17.0.2;Port=8123;Username=default;password=;Database=default");
        await using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO default.TestRuns (TestId) VALUES ('123');";
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Test013()
    {
        await using var connection =
            new ClickHouseConnection("Host=172.17.0.2;Port=8123;Username=default;password=;Database=default");
        var files = Directory.GetFileSystemEntries(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TestData"), "*.csv");
        var dateTime = DateTime.Now;
        foreach (var file in files)
        {
            var buildId = file.Substring(file.IndexOf("Wolfs_Unit_tests_")).Replace("Wolfs_Unit_tests_", "")
                .Replace("-tests.csv", "");
            Console.WriteLine(buildId);
            var lines = TestRunsReader.ReadFromTeamcityTestReport(file);
            dateTime = dateTime.AddDays(-1);
            var uploader = new TestRunsUploader(connection);
            await uploader.UploadAsync("Forms_UnitTests", buildId + "1", "master", lines, dateTime, "KE-FRM-AGENT-01", "Windows");
            await uploader.UploadAsync("Forms_UnitTests", buildId + "2", "tihonove/branch-1", lines, dateTime, "KE-FRM-AGENT-01", "Windows");
            await uploader.UploadAsync("Forms_UnitTests", buildId + "3", "tihonove/branch-2", lines, dateTime, "KE-FRM-AGENT-01", "Linux");
        }
    }

    [Test]
    public async Task Test012()
    {
        await using var connection =
            new ClickHouseConnection("Host=172.17.0.2;Port=8123;Username=default;password=;Database=default");
        
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "default.TestRuns",
            BatchSize = 100
        };
        var values = new [] { new object[] { "jobId", "jobRunId", "branchName", "lines", "Success", (long)123123 } };
        await bulkCopyInterface.WriteToServerAsync(values);
        Console.WriteLine(bulkCopyInterface.RowsWritten);

        // var command = connection.CreateCommand();
        // command.CommandText =
        //     @"insert into TestRuns (JobId, JobRunId, BranchName, TestId, State, Duration) values ({JobId:String, JobRunId:String, BranchName:String, TestId:String, State:Enum8('Success' = 1, 'Failed' = 2, 'Skipped' = 3), Duration:Decimal64(0)});";
        // command.Parameters.Add(new ClickHouseDbParameter { ParameterName = "JobId", Value = "jobId" });
        // command.Parameters.Add(new ClickHouseDbParameter { ParameterName = "JobRunId", Value = "jobRunId" });
        // command.Parameters.Add(new ClickHouseDbParameter { ParameterName = "BranchName", Value = "branchName" });
        // command.Parameters.Add(new ClickHouseDbParameter { ParameterName = "TestId", Value = "lines" });
        // command.Parameters.Add(new ClickHouseDbParameter { ParameterName = "State", Value = "2" });
        // command.Parameters.Add(new ClickHouseDbParameter { ParameterName = "Duration", Value = (long)123445 });
        // await command.ExecuteNonQueryAsync();
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
        await using var connection =
            new ClickHouseConnection("Host=172.17.0.2;Port=8123;Username=default;password=;Database=default");
        var lines = TestRunsReader.ReadFromTeamcityTestReport("/home/tihonove/Downloads/Wolfs_Unit_tests_11509-tests.csv");
        var uploader = new TestRunsUploader(connection);
        await uploader.UploadAsync("Forms_UnitTests", "32028281", "master", lines, DateTime.Now, "AGENT-1", "Windows");
    }

    [Test]
    public async Task Test02()
    {
        var dropTableScript = @"DROP TABLE TestRuns"; 
        Console.WriteLine(dropTableScript);
        
        var createTableScript = @"
            create table default.TestRuns
            (
                JobId String,
                JobRunId String,
                BranchName String,
                TestId String,
                State Enum8('Success' = 1, 'Failed' = 2, 'Skipped' = 3),
                Duration Decimal64(0),
                StartDateTime DateTime,
                AgentName String,
                AgentOSName String
            )
            engine = Memory;
        ";
        Console.WriteLine(createTableScript);
    }
}

public class TestRunsUploader
{
    private readonly ClickHouseConnection connection;

    public TestRunsUploader(ClickHouseConnection connection)
    {
        this.connection = connection;
    }

    public async Task UploadAsync(string jobId, string jobRunId, string branchName, IAsyncEnumerable<TestRun> lines, DateTime dateTimeString, string agentName, string agentOSName)
    {
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "default.TestRuns",
            BatchSize = 100
        };
        await foreach (var testRuns in lines.Batches(100))
        {
            var values=  testRuns.Select(x =>
                new object[] { jobId, jobRunId, branchName, x.TestId, (int)x.TestResult, x.Duration, dateTimeString.ToUniversalTime(), agentName, agentOSName }
            );
            await bulkCopyInterface.WriteToServerAsync(values);
            Console.WriteLine(bulkCopyInterface.RowsWritten);
        }
    }
}

static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<List<T>> Batches<T>(this IAsyncEnumerable<T> items, int batchSize)
    {
        var currentBatch = new List<T>();
        await foreach (var item in items)
        {
            currentBatch.Add(item);
            if (currentBatch.Count >= batchSize)
            {
                yield return currentBatch;
                currentBatch = new List<T>();
            }
        }
        if (currentBatch.Count > 0)
            yield return currentBatch;
    }
}