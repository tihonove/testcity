using ClickHouse.Client.ADO;

namespace Kontur.TestCity.Core.Clickhouse;

public class ConnectionFactory
{
    public ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(GetConnectionString());
    }

    private static string GetConnectionString()
    {
        var connectionStrginBuilder = new ClickHouseConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_HOST is not set"),
            Port = ushort.Parse(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PORT is not set")),
            Database = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_DB is not set"),
            Username = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_USER is not set"),
            Password = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PASSWORD is not set")
        };
        return connectionStrginBuilder.ToString();
    }
}
