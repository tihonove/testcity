using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.GitLab.Models;
using TestCity.Core.Graphite;
using TestCity.Core.JobProcessing;
using TestCity.Core.JUnit;
using TestCity.Core.Storage;
using TestCity.Core.Worker.TaskPayloads;
using TestCity.Worker.Handlers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace TestCity.UnitTests.Handlers;

[TestFixture]
[Explicit]
public class ProcessJobRunTaskHandlerTests
{
    private ProcessJobRunTaskHandler handler;
    private TestMetricsSender metricsSender;
    private SkbKonturGitLabClientProvider gitLabClientProvider;
    private TestCityDatabase testCityDatabase;
    private JUnitExtractor extractor;
    private ProjectJobTypesCache projectJobTypesCache;
    private CommitParentsBuilderService commitParentsBuilder;
    private ILogger logger;

    [SetUp]
    public async Task SetUpAsync()
    {
        logger = GlobalSetup.TestLoggerFactory.CreateLogger<ProcessJobRunTaskHandlerTests>();
        gitLabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        var graphiteClient = new NullGraphiteClient();
        metricsSender = new TestMetricsSender(graphiteClient);
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        testCityDatabase = new TestCityDatabase(connectionFactory);
        extractor = new JUnitExtractor();
        projectJobTypesCache = new ProjectJobTypesCache(testCityDatabase);
        commitParentsBuilder = new CommitParentsBuilderService(gitLabClientProvider, testCityDatabase);
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);

        // Создаем экземпляр тестируемого класса
        handler = new ProcessJobRunTaskHandler(
            metricsSender,
            gitLabClientProvider,
            testCityDatabase,
            extractor,
            projectJobTypesCache,
            commitParentsBuilder);
    }

    [Test]
    public async Task EnqueueAsync_ShouldProcessJob_WhenJobExists2()
    {
        const long projectId = 17358;
        const long jobRunId = 40119524;

        var payload = new ProcessJobRunTaskPayload
        {
            ProjectId = projectId,
            JobRunId = jobRunId
        };

        await handler.EnqueueAsync(payload, CancellationToken.None);
        Assert.That(true, "Метод успешно выполнился");
    }
}
