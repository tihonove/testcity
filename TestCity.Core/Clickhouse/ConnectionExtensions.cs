using System.Data.Common;

namespace Kontur.TestCity.Core.Clickhouse;

public static class ConnectionExtensions
{
    public static async Task<object?> ExecuteScalarAsync(this DbConnection connection, string sql, CancellationToken ct = default)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(ct);
    }

}
