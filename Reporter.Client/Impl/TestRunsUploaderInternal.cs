using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;

namespace Kontur.TestAnalytics.Reporter.Client.Impl;

internal class TestRunsUploaderInternal
{
    private readonly ClickHouseConnection connection;

    public TestRunsUploaderInternal(ClickHouseConnection connection)
    {
        this.connection = connection;
    }

    public async Task UploadAsync(string jobId, string jobRunId, string branchName, IAsyncEnumerable<TestRun> lines,
        string agentName, string agentOSName)
    {
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "TestRuns",
            BatchSize = 1000
        };
        await foreach (var testRuns in lines.Batches(1000))
        {
            var values = testRuns.Select(x =>
                new object[]
                {
                    jobId, jobRunId, branchName, x.TestId, (int)x.TestResult, x.Duration,
                    x.StartDateTime.ToUniversalTime(), agentName, agentOSName
                }
            );
            await bulkCopyInterface.WriteToServerAsync(values);
        }
    }
}