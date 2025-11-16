using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;

namespace TestCity.Clickhouse;

public static class ConnectionExtensions
{
    public static async Task<object?> ExecuteScalarAsync(this DbConnection connection, string sql, CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(ct);
    }

    public static async Task<DbDataReader> ExecuteQueryAsync(this DbConnection connection, string sql, CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteReaderAsync(ct);
    }

    public static async Task<DbDataReader> ExecuteQueryAsyncWithParams(
        this DbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ClickHouseQueryInterpolatedStringHandler query,
        CancellationToken ct = default)
    {
        var chCommand = query.GetCommand();
        return await chCommand.ExecuteReaderAsync(ct);
    }

    [InterpolatedStringHandler]
#pragma warning disable CS9113 // Parameter is unread.
    public class ClickHouseQueryInterpolatedStringHandler(int literalLength, int formattedCount, DbConnection connection)
    {
        private readonly StringBuilder builder = new(literalLength);
        private int parameterIndex;
        private readonly ClickHouseCommand command = connection.CreateCommand() as ClickHouseCommand ?? throw new InvalidOperationException("This interpolated string handler only works with ClickHouse connections");

        public void AppendLiteral(string s)
        {
            builder.Append(s);
        }
        public void AppendFormatted<T>(T value)
        {
            if (value is null)
            {
                builder.Append("NULL");
            }
            else
            {
                var paramName = $"p{parameterIndex}";
                builder.Append($"@{paramName}");
                command.AddParameter(paramName, value);
                parameterIndex++;
            }
        }

        public ClickHouseCommand GetCommand()
        {
            command.CommandText = builder.ToString();
            return command;
        }
    }
#pragma warning restore CS9113 // Parameter is unread.
}
