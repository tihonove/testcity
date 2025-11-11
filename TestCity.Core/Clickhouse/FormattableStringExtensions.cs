using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;

namespace TestCity.Core.Clickhouse;

public static class FormattableStringExtensions
{
    public static async Task<DbDataReader> ExecuteQueryAsyncWithParams(
        this DbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ClickHouseQueryInterpolatedStringHandler query,
        CancellationToken ct = default)
    {
        var chCommand = query.GetCommand();
        return await chCommand.ExecuteReaderAsync(ct);
    }
}

[InterpolatedStringHandler]
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
