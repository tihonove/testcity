using System.Runtime.CompilerServices;
using System.Text;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using TestCity.Core.Clickhouse;
using TestCity.Core.Storage.DTO;

namespace TestCity.Core.Storage;

public class TestCityJobInfo(ConnectionFactory connectionFactory)
{
    public async Task InsertAsync(FullJobInfo info)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        var changesSinceLastRunJson = SerializeChangesSinceLastRun(info.ChangesSinceLastRun);
        var columns = string.Join(", ", Fields2);

        command.CommandText = $@"
            INSERT INTO JobInfo ({columns}) 
            VALUES (
                @JobId, 
                @JobRunId, 
                @BranchName, 
                @AgentName, 
                @AgentOSName, 
                @JobUrl, 
                @State, 
                @Duration, 
                @StartDateTime, 
                @EndDateTime, 
                @Triggered, 
                @PipelineSource,
                @CommitSha, 
                @CommitMessage, 
                @CommitAuthor, 
                @TotalTestsCount, 
                @SuccessTestsCount, 
                @FailedTestsCount, 
                @SkippedTestsCount, 
                @ProjectId, 
                @CustomStatusMessage, 
                @PipelineId, 
                @HasCodeQualityReport,                 
                {changesSinceLastRunJson}
            )";
        command.AddParameter("JobId", info.JobId);
        command.AddParameter("JobRunId", info.JobRunId);
        command.AddParameter("BranchName", info.BranchName ?? string.Empty);
        command.AddParameter("AgentName", info.AgentName);
        command.AddParameter("AgentOSName", info.AgentOSName);
        command.AddParameter("JobUrl", info.JobUrl ?? string.Empty);
        command.AddParameter("State", (int)info.State);
        command.AddParameter("Duration", info.Duration);
        command.AddParameter("StartDateTime", info.StartDateTime.ToUniversalTime());
        command.AddParameter("EndDateTime", info.EndDateTime.ToUniversalTime());
        command.AddParameter("Triggered", info.Triggered ?? string.Empty);
        command.AddParameter("PipelineSource", info.PipelineSource ?? string.Empty);
        command.AddParameter("CommitSha", info.CommitSha ?? string.Empty);
        command.AddParameter("CommitMessage", info.CommitMessage ?? string.Empty);
        command.AddParameter("CommitAuthor", info.CommitAuthor ?? string.Empty);
        command.AddParameter("TotalTestsCount", info.TotalTestsCount);
        command.AddParameter("SuccessTestsCount", info.SuccessTestsCount);
        command.AddParameter("FailedTestsCount", info.FailedTestsCount);
        command.AddParameter("SkippedTestsCount", info.SkippedTestsCount);
        command.AddParameter("ProjectId", info.ProjectId);
        command.AddParameter("CustomStatusMessage", info.CustomStatusMessage ?? string.Empty);
        command.AddParameter("PipelineId", info.PipelineId ?? string.Empty);
        command.AddParameter("HasCodeQualityReport", info.HasCodeQualityReport ? 1 : 0);

        await command.ExecuteNonQueryAsync();
    }
     public async Task InsertAsync(IEnumerable<FullJobInfo> infos)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        var columns = string.Join(", ", Fields2);

        command.CommandText = $@"
            INSERT INTO JobInfo ({columns}) 
            VALUES " + 
        string.Join(", ", infos.Select(info => 
        "(" + string.Join(", ", new [] {
            ToSqlLiteral(info.JobId),
            ToSqlLiteral(info.JobRunId),
            ToSqlLiteral(info.BranchName ?? string.Empty),
            ToSqlLiteral(info.AgentName),
            ToSqlLiteral(info.AgentOSName),
            ToSqlLiteral(info.JobUrl ?? string.Empty),
            ToSqlLiteral(((int)info.State)),
            ToSqlLiteral(info.Duration),
            ToSqlLiteral(info.StartDateTime.ToUniversalTime()),
            ToSqlLiteral(info.EndDateTime.ToUniversalTime()),
            ToSqlLiteral(info.Triggered ?? string.Empty),
            ToSqlLiteral(info.PipelineSource ?? string.Empty),
            ToSqlLiteral(info.CommitSha ?? string.Empty),
            ToSqlLiteral(info.CommitMessage ?? string.Empty),
            ToSqlLiteral(info.CommitAuthor ?? string.Empty),
            ToSqlLiteral(info.TotalTestsCount),
            ToSqlLiteral(info.SuccessTestsCount),
            ToSqlLiteral(info.FailedTestsCount),
            ToSqlLiteral(info.SkippedTestsCount),
            ToSqlLiteral(info.ProjectId),
            ToSqlLiteral(info.CustomStatusMessage ?? string.Empty),
            ToSqlLiteral(info.PipelineId ?? string.Empty),
            ToSqlLiteral(info.HasCodeQualityReport ? 1 : 0),
            SerializeChangesSinceLastRun(info.ChangesSinceLastRun)
        }) + ")"));


        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(string jobRunId)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(JobRunId) > 0 FROM JobInfo WHERE JobRunId = @JobRunId";
        command.AddParameter("JobRunId", jobRunId);
        var result = await command.ExecuteScalarAsync();
        return (byte?)result == 1;
    }

    public async IAsyncEnumerable<string> GetAllJonRunIdsAsync(long projectId, [EnumeratorCancellation] CancellationToken ct)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT DISTINCT JobId FROM JobInfo WHERE ProjectId = @ProjectId";
        command.AddParameter("ProjectId", projectId.ToString());

        var result = await command.ExecuteReaderAsync(ct);

        while (await result.ReadAsync(ct))
        {
            yield return result.GetString(0);
        }
    }

    public async IAsyncEnumerable<FullJobInfo> GetAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var query = $"SELECT {string.Join(", ", Fields2)} FROM JobInfo";
        
        var reader = await connection.ExecuteQueryAsync(query, ct);
        while (await reader.ReadAsync(ct))
        {
            var info = new FullJobInfo
            {
                JobId = reader.GetString(0),
                JobRunId = reader.GetString(1),
                BranchName = reader.GetString(2),
                AgentName = reader.GetString(3),
                AgentOSName = reader.GetString(4),
                JobUrl = reader.GetString(5),
                State = Enum.Parse<JobStatus>(reader.GetString(6)),
                Duration = (long)reader.GetDecimal(7),
                StartDateTime = reader.GetDateTime(8),
                EndDateTime = reader.GetDateTime(9),
                Triggered = reader.GetString(10),
                PipelineSource = reader.GetString(11),
                CommitSha = reader.GetString(12),
                CommitMessage = reader.GetString(13),
                CommitAuthor = reader.GetString(14),
                TotalTestsCount = (int)(uint)reader.GetValue(15),
                SuccessTestsCount = (int)(uint)reader.GetValue(16),
                FailedTestsCount = (int)(uint)reader.GetValue(17),
                SkippedTestsCount = (int)(uint)reader.GetValue(18),
                ProjectId = reader.GetString(19),
                CustomStatusMessage = reader.GetString(20),
                PipelineId = reader.GetString(21),
                HasCodeQualityReport = reader.GetByte(22) > 0
            };

            var changes = (Tuple<string,ushort,string,string,string>[])reader.GetValue(23);
            // Мы не обрабатываем ChangesSinceLastRun, так как это сложная структура в формате JSON
            // При необходимости эту структуру можно парсить из reader.GetString(23)
            if (changes != null)
            {
                info.ChangesSinceLastRun = new List<CommitParentsChangesEntry>();
                foreach (var change in changes)
                {
                    info.ChangesSinceLastRun.Add(new CommitParentsChangesEntry
                    {
                        ParentCommitSha = change.Item1,
                        Depth = change.Item2,
                        AuthorName = change.Item3,
                        AuthorEmail = change.Item4,
                        MessagePreview = change.Item5,
                    });
                }
            }
            else
                info.ChangesSinceLastRun = new List<CommitParentsChangesEntry>();
            
            yield return info;
        }
    }

    private static string SerializeChangesSinceLastRun(List<CommitParentsChangesEntry> changes)
    {
        if (changes.Count == 0)
            return "[]";

        var tuples = new List<string>();
        foreach (var change in changes)
        {
            tuples.Add($"({ToSqlLiteral(change.ParentCommitSha)},{change.Depth},{ToSqlLiteral(change.AuthorName)},{ToSqlLiteral(change.AuthorEmail)},{ToSqlLiteral(change.MessagePreview)})");
        }

        return "[" + string.Join(",", tuples) + "]";
    }

    private static string ToSqlLiteral(string value)
    {
        return $"'{value.Replace("'", "''").Replace(@"\", @"\\").Replace("\n", "\\n").Replace("\r", "\\r")}'";
    }

    private static string ToSqlLiteral(int value)
    {
        return value.ToString();
    }

    private static string ToSqlLiteral(long value)
    {
        return value.ToString();
    }

    private static string ToSqlLiteral(DateTime value)
    {
        return "\'" + value.ToString("yyyy-MM-dd HH:mm:ss") + "\'";
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
        "ChangesSinceLastRun"
    };
}
