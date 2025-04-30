using System.Runtime.CompilerServices;
using ClickHouse.Client.Utility;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Storage.DTO;

namespace Kontur.TestCity.Core.Storage;

public class TestCityGitLabEntities(ConnectionFactory connectionFactory)
{
    public async Task UpsertEntitiesAsync(IEnumerable<GitLabEntityRecord> entities, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO GitLabEntities (Id, Type, Title, ParentId, ParamsJson)
            VALUES (@Id, @Type, @Title, @ParentId, @ParamsJson)";

        foreach (var entity in entities)
        {
            command.Parameters.Clear();
            command.AddParameter("Id", entity.Id);
            command.AddParameter("Type", entity.Type.ToString());
            command.AddParameter("Title", entity.Title);
            command.AddParameter("ParentId", "Nullable(Int64)", entity.ParentId);
            command.AddParameter("ParamsJson", entity.ParamsJson);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async IAsyncEnumerable<GitLabEntityRecord> GetAllEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, argMax(Type, UpdatedAt), argMax(Title, UpdatedAt), argMax(ParentId, UpdatedAt), argMax(ParamsJson, UpdatedAt) FROM GitLabEntities GROUP BY Id";

        var result = new List<GitLabEntityRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new GitLabEntityRecord
            {
                Id = reader.GetInt64(0),
                Type = Enum.Parse<GitLabEntityType>(reader.GetString(1)),
                Title = reader.GetString(2),
                ParentId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                ParamsJson = reader.GetString(4),
            };
        }
    }

    public async Task<bool> IsEmpty(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) == 0 FROM GitLabEntities";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null && (byte)result == 1;
    }

    public async Task<bool> HasProject(long projectId)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) > 0 FROM GitLabEntities WHERE Id = @ProjectId";
        command.AddParameter("ProjectId", projectId);
        var result = await command.ExecuteScalarAsync();
        return result != null && (byte)result == 1;
    }

    public void DeleteById(params long[] groupOrProjectIds)
    {
        if (groupOrProjectIds.Length == 0)
            return;

        var ids = string.Join(", ", groupOrProjectIds);
        var deleteCommand = $"DELETE FROM GitLabEntities WHERE Id IN ({ids})";

        using var connection = connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = deleteCommand;
        command.ExecuteNonQuery();
    }
}
