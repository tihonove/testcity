using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using Kontur.TestAnalytics.Reporter.Client.Impl;
using Kontur.TestCity.Core.Extensions;

namespace Kontur.TestAnalytics.Reporter.Client;

public static class TestRunsUploader
{
    public static async Task UploadAsync(JobRunInfo jobRunInfo, IAsyncEnumerable<TestRun> lines)
    {
        await using var connection = CreateConnection();
        var uploader = new TestRunsUploaderInternal(connection);
        await uploader.UploadAsync(jobRunInfo, lines);
    }

    public static Task UploadAsync(JobRunInfo jobRunInfo, IEnumerable<TestRun> lines)
    {
        return UploadAsync(jobRunInfo, lines.ToAsyncEnumerable());
    }

    public static async Task UploadCommitParents(IEnumerable<CommitParentsEntry> entries, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "CommitParents",
            BatchSize = 1000,
            ColumnNames = [
                "ProjectId",
                "CommitSha",
                "ParentCommitSha",
                "Depth",
                "AuthorName",
                "AuthorEmail",
                "MessagePreview",
            ],
        };
        await bulkCopyInterface.InitAsync();
        await foreach (var entryBatch in entries.ToAsyncEnumerable().Batches(1000))
        {
            var values = entryBatch.Select(x =>
                new object?[]
                {
                    x.ProjectId,
                    x.CommitSha,
                    x.ParentCommitSha,
                    x.Depth,
                    x.AuthorName,
                    x.AuthorEmail,
                    x.MessagePreview,
                });
            await bulkCopyInterface.WriteToServerAsync(values, ct);

        }
    }

    public static async Task<bool> IsJobRunIdExists(string jobId)
    {
        await using var connection = CreateConnection();
        var result = await connection.ExecuteScalarAsync($"Select count(JobRunId) > 0 from JobInfo where JobInfo.JobRunId == '{jobId}'");
        return (byte)result == 1;
    }

    public static async Task JobInfoUploadAsync(FullJobInfo jobInfo)
    {
        await using var connection = CreateConnection();
        var uploader = new JobInfoUploaderInternal(connection);
        await uploader.UploadAsync(jobInfo);
    }

    private static string GetConnectionString()
    {
        var connectionStrginBuilder = new ClickHouseConnectionStringBuilder();
        connectionStrginBuilder.Host = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_HOST is not set");
        connectionStrginBuilder.Port = ushort.Parse(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PORT is not set"));
        connectionStrginBuilder.Database = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_DB is not set");
        connectionStrginBuilder.Username = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_USER is not set");
        connectionStrginBuilder.Password = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PASSWORD is not set");
        return connectionStrginBuilder.ToString();
    }

    public static ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(GetConnectionString());
    }
}
