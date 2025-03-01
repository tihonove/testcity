using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;

namespace Kontur.TestAnalytics.Reporter.Client.Impl;

internal class TestRunsUploaderInternal(ClickHouseConnection connection)
{
    private readonly ClickHouseConnection connection = connection;

    public async Task UploadAsync(JobRunInfo info, IAsyncEnumerable<TestRun> lines)
    {
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
                new object[]
                {
                    info.JobId, info.JobRunId, info.BranchName, x.TestId, (int)x.TestResult, x.Duration,
                    x.StartDateTime.ToUniversalTime(), info.AgentName, info.AgentOSName, info.JobUrl,
                });
            await bulkCopyInterface.WriteToServerAsync(values);
        }
    }

    private static readonly string[] Fields =
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
        "JobUrl",
    };
}
