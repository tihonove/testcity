using TestCity.Core.Clickhouse;
using TestCity.Core.GitLab;
using TestCity.Core.JobProcessing;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;
using NUnit.Framework;

namespace TestCity.UnitTests.Explicits;

[TestFixture]
[Explicit]
public class CommitParentFillTest
{
    private GitLabSettings settings;

    [OneTimeSetUp]
    public void Setup()
    {
        settings = GitLabSettings.Default;
    }

    [Test]
    public async Task FillParents()
    {
        long projectId = 24783;
        var gitLabClientProvider = new SkbKonturGitLabClientProvider(settings);
        var service = new CommitParentsBuilderService(gitLabClientProvider, new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default)));
        await service.BuildCommitParent(projectId, "2d39e9a8868610dd0a09ed8604e1a259db2059de", default);
    }

    [Test]
    public async Task CommitChangesTest()
    {
        // Заданные значения из исходного запроса
        string commitSha = "2e1d2a503fa54bb320972d7aeca6af674001cf5b";
        string jobId = "DotNet tests";
        string branchName = "main";

        // Создаем экземпляр БД
        var database = new TestCityDatabase(new ConnectionFactory(ClickHouseConnectionSettings.Default));

        // Вызываем метод получения изменений
        var changes = await database.GetCommitChangesAsync(commitSha, jobId, branchName);

        // Проверяем полученные данные
        Assert.That(changes, Is.Not.Null);
        Assert.That(changes.Count, Is.EqualTo(3));

        // Проверяем первый результат
        Assert.That(changes[0].ParentCommitSha, Is.EqualTo("2e1d2a503fa54bb320972d7aeca6af674001cf5b"));
        Assert.That(changes[0].Depth, Is.EqualTo(0));
        Assert.That(changes[0].AuthorName, Is.EqualTo("Eugene Tihonov"));
        Assert.That(changes[0].AuthorEmail, Is.EqualTo("tihonov.ea@gmail.com"));
        Assert.That(changes[0].MessagePreview, Is.EqualTo("wip"));

        // Проверяем второй результат
        Assert.That(changes[1].ParentCommitSha, Is.EqualTo("c509e25ca44af26aefc70913c6c7074c8c0b7ceb"));
        Assert.That(changes[1].Depth, Is.EqualTo(1));
        Assert.That(changes[1].AuthorName, Is.EqualTo("Eugene Tihonov"));
        Assert.That(changes[1].AuthorEmail, Is.EqualTo("tihonov.ea@gmail.com"));
        Assert.That(changes[1].MessagePreview, Is.EqualTo("chore: Adjust project names"));

        // Проверяем третий результат
        Assert.That(changes[2].ParentCommitSha, Is.EqualTo("8884205553b17e70b8fd320659d3f3fc1c3b9f6e"));
        Assert.That(changes[2].Depth, Is.EqualTo(2));
        Assert.That(changes[2].AuthorName, Is.EqualTo("Eugene Tihonov"));
        Assert.That(changes[2].AuthorEmail, Is.EqualTo("tihonov.ea@gmail.com"));
        Assert.That(changes[2].MessagePreview, Is.EqualTo("feat: Drop reporter cli"));
    }

    [Test]
    public async Task FullJobInfoInsertWithChangesSinceLastRunTest()
    {
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
        Assert.That(exists, Is.True, "Запись должна существовать в базе данных после вставки");

        // Дополнительные проверки можно добавить, если есть метод для извлечения полной информации о задании
    }

    private static string? GetMessagePreview(GitLabCommit x)
    {
        var firstLine = x.Message?.Split('\n').FirstOrDefault();
        if (firstLine is null)
            return null;

        return firstLine[..Math.Min(100, firstLine.Length)];
    }
}
