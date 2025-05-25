using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.JobProcessing;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Explicits;

[Collection("Global")]
public class CommitParentFillTest
{
    public CommitParentFillTest(ITestOutputHelper output)
    {
        XUnitLoggerProvider.ConfigureTestLogger(output);
    }

    [Fact]
    public async Task FillParents()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;

        long projectId = 24783;
        var gitLabClientProvider = new SkbKonturGitLabClientProvider(settings);
        var service = new CommitParentsBuilderService(gitLabClientProvider, new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default)));
        await service.BuildCommitParent(projectId, "2d39e9a8868610dd0a09ed8604e1a259db2059de", default);
    }

    [Fact]
    public async Task CommitChangesTest()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;

        // Заданные значения из исходного запроса
        string commitSha = "2e1d2a503fa54bb320972d7aeca6af674001cf5b";
        string jobId = "DotNet tests";
        string branchName = "main";

        // Создаем экземпляр БД
        var database = new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default));

        // Вызываем метод получения изменений
        var changes = await database.GetCommitChangesAsync(commitSha, jobId, branchName);

        // Проверяем полученные данные
        Assert.NotNull(changes);
        Assert.Equal(3, changes.Count);

        // Проверяем первый результат
        Assert.Equal("2e1d2a503fa54bb320972d7aeca6af674001cf5b", changes[0].ParentCommitSha);
        Assert.Equal(0, changes[0].Depth);
        Assert.Equal("Eugene Tihonov", changes[0].AuthorName);
        Assert.Equal("tihonov.ea@gmail.com", changes[0].AuthorEmail);
        Assert.Equal("wip", changes[0].MessagePreview);

        // Проверяем второй результат
        Assert.Equal("c509e25ca44af26aefc70913c6c7074c8c0b7ceb", changes[1].ParentCommitSha);
        Assert.Equal(1, changes[1].Depth);
        Assert.Equal("Eugene Tihonov", changes[1].AuthorName);
        Assert.Equal("tihonov.ea@gmail.com", changes[1].AuthorEmail);
        Assert.Equal("chore: Adjust project names", changes[1].MessagePreview);

        // Проверяем третий результат
        Assert.Equal("8884205553b17e70b8fd320659d3f3fc1c3b9f6e", changes[2].ParentCommitSha);
        Assert.Equal(2, changes[2].Depth);
        Assert.Equal("Eugene Tihonov", changes[2].AuthorName);
        Assert.Equal("tihonov.ea@gmail.com", changes[2].AuthorEmail);
        Assert.Equal("feat: Drop reporter cli", changes[2].MessagePreview);
    }

    [Fact]
    public async Task FullJobInfoInsertWithChangesSinceLastRunTest()
    {
        if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
            return;

        // Создаем уникальные идентификаторы для изоляции теста
        var jobId = $"test-job-{Guid.NewGuid()}";
        var jobRunId = $"test-run-{Guid.NewGuid()}";
        var projectId = "test-project-id";

        // Создаем список изменений
        var changesList = new List<CommitParentsChangesEntry>
        {
            new()
            {
                ParentCommitSha = "abc123",
                Depth = 0,
                AuthorName = "Test Author",
                AuthorEmail = "test@example.com",
                MessagePreview = "First commit message"
            },
            new()
            {
                ParentCommitSha = "def456",
                Depth = 1,
                AuthorName = "Another Author",
                AuthorEmail = "another@example.com",
                MessagePreview = "Second commit message"
            }
        };

        // Создаем объект FullJobInfo с заполненным списком ChangesSinceLastRun
        var jobInfo = new FullJobInfo
        {
            JobId = jobId,
            JobRunId = jobRunId,
            BranchName = "test-branch",
            AgentName = "test-agent",
            AgentOSName = "Linux",
            JobUrl = "https://example.com/job",
            State = JobStatus.Success,
            Duration = 60,
            StartDateTime = DateTime.UtcNow.AddMinutes(-1),
            EndDateTime = DateTime.UtcNow,
            Triggered = "manual",
            PipelineSource = "web",
            CommitSha = "abc123",
            CommitMessage = "Test commit message",
            CommitAuthor = "Test Author",
            TotalTestsCount = 100,
            SuccessTestsCount = 95,
            FailedTestsCount = 3,
            SkippedTestsCount = 2,
            ProjectId = projectId,
            CustomStatusMessage = "Test completed",
            PipelineId = "12345",
            HasCodeQualityReport = true,
            ChangesSinceLastRun = changesList
        };

        // Создаем экземпляр TestCityJobInfo для работы с базой данных
        var connectionFactory = new ConnectionFactory(ClickHouseConnectionSettings.Default);
        var testCityJobInfo = new TestCityJobInfo(connectionFactory);

        // Вставляем данные
        await testCityJobInfo.InsertAsync(jobInfo);

        // Проверяем, что данные были сохранены
        var exists = await testCityJobInfo.ExistsAsync(jobRunId);
        Assert.True(exists, "Запись должна существовать в базе данных после вставки");

        // Дополнительные проверки можно добавить, если есть метод для извлечения полной информации о задании
    }

    private static string? GetMessagePreview(GitLabCommit x)
    {
        var firstLine = x.Message?.Split('\n').FirstOrDefault();
        if (firstLine is null)
            return null;

        return firstLine[..Math.Min(100, firstLine.Length)];
    }
    private readonly GitLabSettings settings = GitLabSettings.Default;
}
