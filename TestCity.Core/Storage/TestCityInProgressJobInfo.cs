using ClickHouse.Client.Utility;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.Storage;

public class TestCityInProgressJobInfo(ConnectionFactory connectionFactory)
{
    public async Task InsertAsync(InProgressJobInfo info)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        var changesSinceLastRunJson = SerializeChangesSinceLastRun(info.ChangesSinceLastRun);
        var columns = string.Join(", ", Fields);

        command.CommandText = $@"
            INSERT INTO InProgressJobInfo ({columns}) 
            VALUES (
                @JobId, 
                @JobRunId, 
                @JobUrl, 
                @StartDateTime, 
                @PipelineSource, 
                @Triggered, 
                @BranchName, 
                @CommitSha, 
                @CommitMessage, 
                @CommitAuthor, 
                @AgentName, 
                @AgentOSName, 
                @ProjectId, 
                @PipelineId,
                @JobStatus,
                @LastUpdateTime,
                {changesSinceLastRunJson}
            )";

        command.AddParameter("JobId", info.JobId);
        command.AddParameter("JobRunId", info.JobRunId);
        command.AddParameter("JobUrl", info.JobUrl ?? string.Empty);
        command.AddParameter("StartDateTime", info.StartDateTime.ToUniversalTime());
        command.AddParameter("PipelineSource", info.PipelineSource ?? string.Empty);
        command.AddParameter("Triggered", info.Triggered ?? string.Empty);
        command.AddParameter("BranchName", info.BranchName ?? string.Empty);
        command.AddParameter("CommitSha", info.CommitSha ?? string.Empty);
        command.AddParameter("CommitMessage", info.CommitMessage ?? string.Empty);
        command.AddParameter("CommitAuthor", info.CommitAuthor ?? string.Empty);
        command.AddParameter("AgentName", info.AgentName ?? string.Empty);
        command.AddParameter("AgentOSName", info.AgentOSName ?? string.Empty);
        command.AddParameter("ProjectId", info.ProjectId);
        command.AddParameter("PipelineId", info.PipelineId ?? string.Empty);
        command.AddParameter("JobStatus", info.JobStatus);
        command.AddParameter("LastUpdateTime", info.LastUpdateTime.ToUniversalTime());

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(string projectId, string jobRunId)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) > 0 FROM InProgressJobInfo WHERE ProjectId = @ProjectId AND JobRunId = @JobRunId";
        command.AddParameter("ProjectId", projectId);
        command.AddParameter("JobRunId", jobRunId);
        var result = await command.ExecuteScalarAsync();
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
        "PipelineId",
        "JobStatus",
        "LastUpdateTime",
        "ChangesSinceLastRun"
    };

    private static string SerializeChangesSinceLastRun(List<CommitParentsChangesEntry> changes)
    {
        if (changes.Count == 0)
            return "[]";

        var tuples = new List<string>();
        foreach (var change in changes)
        {
            tuples.Add($"('{change.ParentCommitSha.Replace("'", "''")}',{change.Depth},'{change.AuthorName.Replace("'", "''")}','{change.AuthorEmail.Replace("'", "''")}','{change.MessagePreview.Replace("'", "''")}')");
        }

        return "[" + string.Join(",", tuples) + "]";
    }

    public async Task<List<InProgressJobInfo>> GetAllByProjectIdAsync(string projectId)
    {
        var result = new List<InProgressJobInfo>();
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        var columns = string.Join(", ", Fields.Take(14)); // Берем только первые 14 полей, которые были изначально
        command.CommandText = $"SELECT {columns}, JobStatus, LastUpdateTime FROM InProgressJobInfo WHERE ProjectId = @ProjectId";
        command.AddParameter("ProjectId", projectId);

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
                PipelineId = reader.IsDBNull(13) ? null : reader.GetString(13),
                JobStatus = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                LastUpdateTime = reader.IsDBNull(15) ? DateTime.UtcNow : reader.GetDateTime(15)
                // ChangesSinceLastRun не вычитываем, так как оно используется только при вставке
            };
            result.Add(info);
        }
        return result;
    }
}
