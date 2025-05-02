using System.Runtime.CompilerServices;
using System.Text;
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

    private static string SerializeChangesSinceLastRun(List<CommitParentsChangesEntry> changes)
    {
        if (changes.Count == 0)
            return "[]";

        var tuples = new List<string>();
        foreach (var change in changes)
        {
            tuples.Add($"({ToSqlStringLiteral(change.ParentCommitSha)},{change.Depth},{ToSqlStringLiteral(change.AuthorName)},{ToSqlStringLiteral(change.AuthorEmail)},{ToSqlStringLiteral(change.MessagePreview)})");
        }

        return "[" + string.Join(",", tuples) + "]";
    }

    private static string ToSqlStringLiteral(string value)
    {
        return $"'{value.Replace("'", "''").Replace(@"\", @"\\")}'";
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
