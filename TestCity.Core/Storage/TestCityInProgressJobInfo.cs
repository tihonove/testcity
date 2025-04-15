using ClickHouse.Client.Copy;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.Storage;

public class TestCityInProgressJobInfo(ConnectionFactory connectionFactory)
{
    public async Task InsertAsync(InProgressJobInfo info)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "InProgressJobInfo",
            BatchSize = 1,
            ColumnNames = Fields,
        };
        await bulkCopyInterface.InitAsync();

        await bulkCopyInterface.WriteToServerAsync(
            [
                [
                    info.JobId,
                    info.JobRunId,
                    info.JobUrl,
                    info.StartDateTime.ToUniversalTime(),
                    info.PipelineSource,
                    info.Triggered,
                    info.BranchName,
                    info.CommitSha,
                    info.CommitMessage,
                    info.CommitAuthor,
                    info.AgentName,
                    info.AgentOSName,
                    info.ProjectId,
                    info.PipelineId
                ],
            ]);
    }
    
    public async Task<bool> ExistsAsync(string projectId, string jobRunId)
    {
        await using var connection = connectionFactory.CreateConnection();
        var result = await connection.ExecuteScalarAsync($"SELECT count(*) > 0 FROM InProgressJobInfo WHERE ProjectId = '{projectId}' AND JobRunId = '{jobRunId}'");
        return result != null && (byte)result == 1;
    }

    private static readonly string[] Fields =
    {
        "JobId",
        "JobRunId",
        "JobUrl",
        "StartDateTime",
        "PipelineSource",
        "Triggered",
        "BranchName",
        "CommitSha",
        "CommitMessage",
        "CommitAuthor",
        "AgentName",
        "AgentOSName",
        "ProjectId",
        "PipelineId"
    };
}
