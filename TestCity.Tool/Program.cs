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
logger.LogInformation("Logger initialized.");

var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
var clientEx = clientProvider.GetExtendedClient();
var projects = GitLabProjectsService.GetAllProjects();
var client = clientProvider.GetClient();
var messageQueueClient = KafkaMessageQueueClient.CreateDefault(loggerFactory.CreateLogger<KafkaMessageQueueClient>());
var workreClient = new WorkerClient(messageQueueClient);

foreach (var project in projects)
{
    await foreach (var commit in clientEx.GetAllRepositoryCommitsAsync(long.Parse(project.Id)).TakeWhile(x => x.CommittedDate > DateTime.Now.AddMonths(-2)))
    {
        await workreClient.Enqueue(new BuildCommitParentsTaskPayload { ProjectId = long.Parse(project.Id), CommitSha = commit.Id });
    }
}
