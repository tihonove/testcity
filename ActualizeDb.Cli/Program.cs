using dotenv.net;
using TestCity.Clickhouse;
using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.Worker;

DotEnv.Fluent().WithProbeForEnv(10).Load();

var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
await using var connection = connectionFactory.CreateConnection();
await connection.EnsureDbIsAccessibleAsync(TimeSpan.FromMinutes(20));

await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
Console.WriteLine("Database schema actualized successfully.");
var gitLabSettings = GitLabSettings.Default;


if (Environment.GetCommandLineArgs().Contains("--add-predefined-projects"))
{
    if (gitLabSettings.Url.ToString().Contains("gitlab.com"))
    {
        await TestAnalyticsDatabaseSchema.InsertPredefinedGitLabProjects(connectionFactory);
    }
    else
    {
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }
    Console.WriteLine("Actualized predefined projects.");
}

await KafkaTopicActualizer.EnsureKafkaTopicExists();
Console.WriteLine("Kafka topics verified successfully.");
