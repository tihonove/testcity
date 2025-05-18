using ClickHouse.Client.Copy;
using TestCity.Core.Clickhouse;
using TestCity.Core.Extensions;
using TestCity.Core.Storage.DTO;
using System.Runtime.CompilerServices;

namespace TestCity.Core.Storage;

public class TestCityTestRuns(ConnectionFactory connectionFactory)
{
    public Task InsertBatchAsync(JobRunInfo jobRunInfo, IEnumerable<TestRun> lines)
    {
        return InsertBatchAsync(jobRunInfo, lines.ToAsyncEnumerable());
    }

    public async Task InsertBatchAsync(JobRunInfo info, IAsyncEnumerable<TestRun> lines)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "TestRuns",
            BatchSize = 1000,
            ColumnNames = Fields,
        };
        await bulkCopyInterface.InitAsync();
        await foreach (var testRuns in lines.Batches(1000))
        {
            var values = testRuns.Select(x =>
                new object?[]
                {
                    info.JobId, info.JobRunId, info.ProjectId, info.BranchName, x.TestId, (int)x.TestResult, x.Duration,
                    x.StartDateTime.ToUniversalTime(), info.AgentName, info.AgentOSName, info.JobUrl,
                    x.JUnitFailureMessage, x.JUnitFailureOutput, x.JUnitSystemOutput,
                });
            await bulkCopyInterface.WriteToServerAsync(values);
        }
    }

    public async Task InsertBatchAsync(IAsyncEnumerable<(JobRunInfo, TestRun)> lines)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "TestRuns",
            BatchSize = 1000,
            ColumnNames = Fields,
        };
        await bulkCopyInterface.InitAsync();
        await foreach (var testRunLine in lines.Batches(1000))
        {
            var values = testRunLine.Select(x =>
                new object?[]
                {
                    x.Item1.JobId, x.Item1.JobRunId, x.Item1.ProjectId, x.Item1.BranchName, x.Item2.TestId, (int)x.Item2.TestResult, x.Item2.Duration,
                    x.Item2.StartDateTime.ToUniversalTime(), x.Item1.AgentName, x.Item1.AgentOSName, x.Item1.JobUrl,
                    x.Item2.JUnitFailureMessage, x.Item2.JUnitFailureOutput, x.Item2.JUnitSystemOutput,
                });
            await bulkCopyInterface.WriteToServerAsync(values);
        }
    }

    public async IAsyncEnumerable<(JobRunInfo, TestRun)> GetAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var query = @"
            SELECT
                tr.JobId,
                tr.JobRunId,
                tr.ProjectId,
                tr.BranchName,
                tr.TestId,
                tr.State,
                tr.Duration,
                tr.StartDateTime,
                tr.AgentName,
                tr.AgentOSName,
                tr.JobUrl,
                tr.JUnitFailureMessage,
                tr.JUnitFailureOutput,
                tr.JUnitSystemOutput
            FROM TestRuns tr
        ";

        var reader = await connection.ExecuteQueryAsync(query, ct);
        while (await reader.ReadAsync(ct))
        {
            var jobRunInfo = new JobRunInfo
            {
                JobId = reader.GetString(0),
                JobRunId = reader.GetString(1),
                ProjectId = reader.GetString(2),
                BranchName = reader.GetString(3),
                AgentName = reader.GetString(8),
                AgentOSName = reader.GetString(9),
                JobUrl = reader.GetString(10),
                PipelineId = string.Empty // Дополнительное поле, требуемое для JobRunInfo
            };

            var testRun = new TestRun
            {
                TestId = reader.GetString(4),
                TestResult = Enum.Parse<TestResult>(reader.GetString(5)),
                Duration = (long)reader.GetDecimal(6),
                StartDateTime = reader.GetDateTime(7),
                JUnitFailureMessage = reader.IsDBNull(11) ? null : reader.GetString(11),
                JUnitFailureOutput = reader.IsDBNull(12) ? null : reader.GetString(12),
                JUnitSystemOutput = reader.IsDBNull(13) ? null : reader.GetString(13)
            };

            yield return (jobRunInfo, testRun);
        }
    }


    private static readonly string[] Fields =
    [
        "JobId",
        "JobRunId",
        "ProjectId",
        "BranchName",
        "TestId",
        "State",
        "Duration",
        "StartDateTime",
        "AgentName",
        "AgentOSName",
        "JobUrl",
        "JUnitFailureMessage",
        "JUnitFailureOutput",
        "JUnitSystemOutput",
    ];
}
