using ClickHouse.Client.Copy;
using TestCity.Core.Clickhouse;
using TestCity.Core.Extensions;
using TestCity.Core.Storage.DTO;

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
