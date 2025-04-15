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

    public async Task<List<InProgressJobInfo>> GetAllByProjectIdAsync(string projectId)
    {
        var result = new List<InProgressJobInfo>();
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $@"SELECT JobId, JobRunId, JobUrl, StartDateTime, PipelineSource, Triggered, BranchName, CommitSha, CommitMessage, CommitAuthor, AgentName, AgentOSName, ProjectId, PipelineId FROM InProgressJobInfo WHERE ProjectId = '{projectId}'";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var info = new InProgressJobInfo
            {
                JobId = reader.GetString(0),
                JobRunId = reader.GetString(1),
                JobUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
                StartDateTime = reader.GetDateTime(3),
                PipelineSource = reader.IsDBNull(4) ? null : reader.GetString(4),
                Triggered = reader.IsDBNull(5) ? null : reader.GetString(5),
                BranchName = reader.IsDBNull(6) ? null : reader.GetString(6),
                CommitSha = reader.IsDBNull(7) ? null : reader.GetString(7),
                CommitMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
                CommitAuthor = reader.IsDBNull(9) ? null : reader.GetString(9),
                AgentName = reader.IsDBNull(10) ? null : reader.GetString(10),
                AgentOSName = reader.IsDBNull(11) ? null : reader.GetString(11),
                ProjectId = reader.GetString(12),
                PipelineId = reader.IsDBNull(13) ? null : reader.GetString(13)
            };
            result.Add(info);
        }
        return result;
    }
}
