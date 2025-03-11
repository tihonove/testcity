using ClickHouse.Client.ADO;

namespace Kontur.TestAnalytics.Core.Clickhouse;

public static class ClickHouseConnectionExtensions
{
    public static async Task EnsureDbIsAccessibleAsync(this ClickHouseConnection connection, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteNonQueryAsync();
                return;
            }
            catch
            {
                Console.WriteLine($"Clickhouse DB {connection.Database} is not accessible yet. Retrying...");
                await Task.Delay(500);
            }
        }

        throw new TimeoutException("DB not accessible within the specified timeout.");
    }
}
