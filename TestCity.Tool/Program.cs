using System.Text;
using dotenv.net;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
var clientEx = clientProvider.GetExtendedClient();
var projects = GitLabProjectsService.GetAllProjects();
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
        await foreach (var commit in clientEx.GetAllRepositoryCommitsAsync(long.Parse(project.Id)).TakeWhile(x => x.CommittedDate > DateTime.Now.AddMonths(-2)))
        {
            logger.LogDebug("Processing commit {CommitSha} from {CommitDate} by {AuthorName}",
                commit.Id, commit.CommittedDate, commit.AuthorName);

            try
            {
                await workreClient.Enqueue(new BuildCommitParentsTaskPayload { ProjectId = long.Parse(project.Id), CommitSha = commit.Id });
                logger.LogDebug("Successfully enqueued task for commit {CommitSha}", commit.Id);
                commitCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue task for commit {CommitSha} in project {ProjectId}", commit.Id, project.Id);
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
