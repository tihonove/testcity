using ClickHouse.Client.ADO;

namespace Kontur.TestCity.Core.Clickhouse;

public static class ConnectionFactory
{
    public static string GetConnectionString()
    {
        var connectionStrginBuilder = new ClickHouseConnectionStringBuilder();
        connectionStrginBuilder.Host = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_HOST is not set");
        connectionStrginBuilder.Port = ushort.Parse(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PORT is not set"));
        connectionStrginBuilder.Database = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_DB is not set");
        connectionStrginBuilder.Username = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_USER is not set");
        connectionStrginBuilder.Password = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PASSWORD is not set");
        return connectionStrginBuilder.ToString();
    }

    public static ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(GetConnectionString());
    }
}
