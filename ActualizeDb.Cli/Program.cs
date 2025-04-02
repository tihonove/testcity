using System;
using System.Threading.Tasks;
using dotenv.net;
using Kontur.TestAnalytics.ActualizeDb.Cli;
using Kontur.TestCity.Core.Clickhouse;

DotEnv.Fluent().WithProbeForEnv(10).Load();

var connection = ConnectionFactory.CreateConnection();
await connection.EnsureDbIsAccessibleAsync(TimeSpan.FromMinutes(20));

await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
Console.WriteLine("Database schema actualized successfully.");

await KafkaTopicActualizer.EnsureKafkaTopicExists();
Console.WriteLine("Kafka topics verified successfully.");
