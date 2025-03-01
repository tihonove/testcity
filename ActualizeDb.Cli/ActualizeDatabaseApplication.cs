using dotenv.net;
using Kontur.TestAnalytics.Core.Clickhouse;
using Vostok.Hosting.Abstractions;

namespace Kontur.TestAnalytics.ActualizeDb.Cli;

public class ActualizeDatabaseApplication : IVostokApplication
{
    public Task InitializeAsync(IVostokHostingEnvironment environment) => Task.CompletedTask;

    public async Task RunAsync(IVostokHostingEnvironment environment)
    {
        DotEnv.Fluent().WithProbeForEnv(10).Load();
        var connection = ConnectionFactory.CreateConnection();
        await connection.EnsureDbIsAccessibleAsync(TimeSpan.FromMinutes(20));

        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        Console.WriteLine("Database schema actualized successfully.");
    }
}
