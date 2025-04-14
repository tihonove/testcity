using dotenv.net;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.Worker;

DotEnv.Fluent().WithProbeForEnv(10).Load();

var connection = new ConnectionFactory().CreateConnection();
await connection.EnsureDbIsAccessibleAsync(TimeSpan.FromMinutes(20));

await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
Console.WriteLine("Database schema actualized successfully.");

await KafkaTopicActualizer.EnsureKafkaTopicExists();
Console.WriteLine("Kafka topics verified successfully.");
