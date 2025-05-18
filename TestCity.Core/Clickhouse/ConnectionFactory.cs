using ClickHouse.Client.ADO;

namespace TestCity.Core.Clickhouse;

public class ConnectionFactory(ClickHouseConnectionSettings connectionSettings)
{
    public ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(GetConnectionString(connectionSettings));
    }

    private static string GetConnectionString(ClickHouseConnectionSettings connectionSettings)
    {
        var connectionStrginBuilder = new ClickHouseConnectionStringBuilder
        {
            Host = connectionSettings.Host,
            Port = connectionSettings.Port,
            Database = connectionSettings.Database,
            Username = connectionSettings.Username,
            Password = connectionSettings.Password,
        };
        return connectionStrginBuilder.ToString();
    }
}
