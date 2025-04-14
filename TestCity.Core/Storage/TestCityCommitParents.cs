using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Extensions;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.Storage;

public class TestCityCommitParents(ConnectionFactory connectionFactory)
{
    public async Task InsertBatchAsync(IEnumerable<CommitParentsEntry> entries, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "CommitParents",
            BatchSize = 1000,
            ColumnNames = [
                "ProjectId",
                "CommitSha",
                "ParentCommitSha",
                "Depth",
                "AuthorName",
                "AuthorEmail",
                "MessagePreview",
            ],
        };
        await bulkCopyInterface.InitAsync();
        await foreach (var entryBatch in entries.ToAsyncEnumerable().Batches(1000))
        {
            var values = entryBatch.Select(x =>
                new object?[]
                {
                    x.ProjectId,
                    x.CommitSha,
                    x.ParentCommitSha,
                    x.Depth,
                    x.AuthorName,
                    x.AuthorEmail,
                    x.MessagePreview,
                });
            await bulkCopyInterface.WriteToServerAsync(values, ct);
        }
    }

    public async Task<bool> ExistsAsync(long projectId, string commitSha, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var query = $"SELECT count() > 0 FROM CommitParents WHERE ProjectId = '{projectId}' AND CommitSha = '{commitSha}' AND Depth = 0";
        var result = await connection.ExecuteScalarAsync(query, ct);
        return result != null && (byte)result > 0;
    }
}
