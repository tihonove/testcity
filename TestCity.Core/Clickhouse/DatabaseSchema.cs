using System.Reflection;
using ClickHouse.Client.ADO;

namespace Kontur.TestCity.Core.Clickhouse;

public class TestAnalyticsDatabaseSchema
{
    public static async Task ActualizeDatabaseSchemaAsync(ClickHouseConnection connection)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{typeof(TestAnalyticsDatabaseSchema).Namespace}.SchemaMigrations.Schema1.sql";
        await using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"Resource {resourceName} not found");
        using var reader = new StreamReader(stream);
        var sqlScript = await reader.ReadToEndAsync();
        var statements = sqlScript.Split(new[] { "-- divider --" }, StringSplitOptions.RemoveEmptyEntries);
        await using var command = connection.CreateCommand();
        foreach (var statement in statements)
        {
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }
    }
}
