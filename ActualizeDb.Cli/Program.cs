using dotenv.net;
using TestCity.Core.Clickhouse;
using TestCity.Core.Worker;

DotEnv.Fluent().WithProbeForEnv(10).Load();

var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
await using var connection = connectionFactory.CreateConnection();
await connection.EnsureDbIsAccessibleAsync(TimeSpan.FromMinutes(20));

await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
Console.WriteLine("Database schema actualized successfully.");

// if (Environment.GetCommandLineArgs().Contains("--add-predefined-projects"))
// {
//     await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
//     Console.WriteLine("Actualized predefined projects.");
// }

await KafkaTopicActualizer.EnsureKafkaTopicExists();
Console.WriteLine("Kafka topics verified successfully.");
