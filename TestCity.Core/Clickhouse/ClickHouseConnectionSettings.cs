namespace TestCity.Core.Clickhouse;

public class ClickHouseConnectionSettings
{
    public required string Host { get; init; }
    public required ushort Port { get; set; }
    public required string Database { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }

    public static ClickHouseConnectionSettings CreateDefaultFromEnvironment()
    {
        return new ClickHouseConnectionSettings
        {
            Host = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_HOST is not set"),
            Port = ushort.Parse(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PORT is not set")),
            Database = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_DB is not set"),
            Username = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_USER is not set"),
            Password = Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD") ?? throw new Exception("TESTANALYTICS_CLICKHOUSE_PASSWORD is not set")
        };
    }

    public static ClickHouseConnectionSettings Default => CreateDefaultFromEnvironment();
}
