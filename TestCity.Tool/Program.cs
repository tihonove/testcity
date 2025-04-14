using System.Text;
using ClickHouse.Client.Utility;
using dotenv.net;
using Kontur.TestCity.Core.Clickhouse;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitlabProjects;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();
Log.ConfigureGlobalLogProvider(loggerFactory);

var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
var clientEx = clientProvider.GetExtendedClient();
var projects = PreconfiguredGitLabProjectsService.GetAllProjects();
var client = clientProvider.GetClient();
var messageQueueClient = KafkaMessageQueueClient.CreateDefault(loggerFactory.CreateLogger<KafkaMessageQueueClient>());
var workreClient = new WorkerClient(messageQueueClient);

logger.LogInformation("Starting to process projects");
foreach (var project in projects)
{
    logger.LogInformation("Processing project {ProjectId}: {ProjectTitle}", project.Id, project.Title);
    int commitCount = 0;

    try
    {
        var connectionFactory = new ConnectionFactory();
        var connection = connectionFactory.CreateConnection();
        var query = $"SELECT DISTINCT CommitSha FROM JobInfo WHERE ProjectId = '{project.Id}'";
        using var reader = await connection.ExecuteReaderAsync(query);

        var commitHashes = new List<string>();
        while (await reader.ReadAsync())
        {
            var commitSha = reader.GetString(0);
            commitHashes.Add(commitSha);
        }

        logger.LogInformation("Found {CommitCount} unique commits in ClickHouse for project {ProjectId}",
            commitHashes.Count, project.Id);

        foreach (var commitSha in commitHashes)
        {
            logger.LogDebug("Processing commit {CommitSha} from ClickHouse", commitSha);

            try
            {
                await workreClient.Enqueue(new BuildCommitParentsTaskPayload
                {
                    ProjectId = long.Parse(project.Id),
                    CommitSha = commitSha
                });
                logger.LogDebug("Successfully enqueued task for commit {CommitSha}", commitSha);
                commitCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue task for commit {CommitSha} in project {ProjectId}",
                    commitSha, project.Id);
            }
        }

        logger.LogInformation("Processed {CommitCount} commits for project {ProjectId}", commitCount, project.Id);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing project {ProjectId}: {ProjectTitle}", project.Id, project.Title);
    }
}

logger.LogInformation("Processing completed for all projects");
