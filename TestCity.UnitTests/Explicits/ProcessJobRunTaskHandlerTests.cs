using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.Graphite;
using TestCity.Core.JobProcessing;
using TestCity.Core.JUnit;
using TestCity.Core.Storage;
using TestCity.Core.Worker.TaskPayloads;
using TestCity.Worker.Handlers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Handlers;

[Collection("Global")]
public class ProcessJobRunTaskHandlerTests : IAsyncLifetime
{
    private readonly ProcessJobRunTaskHandler handler;
    private readonly TestMetricsSender metricsSender;
    private readonly ConnectionFactory connectionFactory;
    private readonly SkbKonturGitLabClientProvider gitLabClientProvider;
    private readonly TestCityDatabase testCityDatabase;
    private readonly JUnitExtractor extractor;
    private readonly ProjectJobTypesCache projectJobTypesCache;
    private readonly CommitParentsBuilderService commitParentsBuilder;
    private readonly ILogger logger;

    public ProcessJobRunTaskHandlerTests(ITestOutputHelper output)
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        logger = GlobalSetup.TestLoggerFactory(output).CreateLogger<ProcessJobRunTaskHandlerTests>();
        gitLabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        var graphiteClient = new NullGraphiteClient();
        metricsSender = new TestMetricsSender(graphiteClient);
        connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        testCityDatabase = new TestCityDatabase(connectionFactory);
        extractor = new JUnitExtractor();
        projectJobTypesCache = new ProjectJobTypesCache(testCityDatabase);
        commitParentsBuilder = new CommitParentsBuilderService(gitLabClientProvider, testCityDatabase);
        handler = new ProcessJobRunTaskHandler(
            metricsSender,
            gitLabClientProvider,
            testCityDatabase,
            extractor,
            projectJobTypesCache,
            commitParentsBuilder);
    }

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;
        await using var connection = connectionFactory.CreateConnection();
        await TestAnalyticsDatabaseSchema.ActualizeDatabaseSchemaAsync(connection);
        await TestAnalyticsDatabaseSchema.InsertPredefinedProjects(connectionFactory);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task EnqueueAsync_ShouldProcessJob_WhenJobExists2()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;

        const long projectId = 17358;
        const long jobRunId = 40119524;

        var payload = new ProcessJobRunTaskPayload
        {
            ProjectId = projectId,
            JobRunId = jobRunId
        };

        await handler.EnqueueAsync(payload, CancellationToken.None);
        Assert.True(true, "Метод успешно выполнился");
    }
}
