using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.Storage;

public class TestCityJobInfo(ConnectionFactory connectionFactory)
{
    public async Task InsertAsync(FullJobInfo info)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "JobInfo",
            BatchSize = 1,
            ColumnNames = Fields2,
        };
        await bulkCopyInterface.InitAsync();

        await bulkCopyInterface.WriteToServerAsync(
            [
                [
                    info.JobId,
                    info.JobRunId,
                    info.BranchName,
                    info.AgentName,
                    info.AgentOSName,
                    info.JobUrl,
                    info.State,
                    info.Duration,
                    info.StartDateTime.ToUniversalTime(),
                    info.EndDateTime.ToUniversalTime(),
                    info.Triggered,
                    info.PipelineSource,
                    info.CommitSha,
                    info.CommitMessage,
                    info.CommitAuthor,
                    info.TotalTestsCount,
                    info.SuccessTestsCount,
                    info.FailedTestsCount,
                    info.SkippedTestsCount,
                    info.ProjectId,
                    info.CustomStatusMessage,
                    info.PipelineId,
                    info.HasCodeQualityReport ? 1 : 0,
                ],
            ]);
    }

    public async Task<bool> ExistsAsync(string jobId)
    {
        await using var connection = connectionFactory.CreateConnection();
        var result = await connection.ExecuteScalarAsync($"Select count(JobRunId) > 0 from JobInfo where JobInfo.JobRunId == '{jobId}'");
        return (byte)result == 1;
    }

    private static readonly string[] Fields2 =
    {
        "JobId",
        "JobRunId",
        "BranchName",
        "AgentName",
        "AgentOSName",
        "JobUrl",
        "State",
        "Duration",
        "StartDateTime",
        "EndDateTime",
        "Triggered",
        "PipelineSource",
        "CommitSha",
        "CommitMessage",
        "CommitAuthor",
        "TotalTestsCount",
        "SuccessTestsCount",
        "FailedTestsCount",
        "SkippedTestsCount",
        "ProjectId",
        "CustomStatusMessage",
        "PipelineId",
        "HasCodeQualityReport",
    };
}
