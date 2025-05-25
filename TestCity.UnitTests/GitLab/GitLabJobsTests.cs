using TestCity.Core.GitLab;
using TestCity.Core.GitLab.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using TestCity.UnitTests.Utils;
using Xunit.Abstractions;

namespace TestCity.UnitTests.GitLab;

[Collection("Global")]
public class GitLabJobsTests : IDisposable
{
    public GitLabJobsTests(ITestOutputHelper output)
    {
        CIUtils.SkipOnGitHubActions();
        logger = GlobalSetup.TestLoggerFactory(output).CreateLogger<GitLabJobsTests>();
        var provider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        gitLabClient = provider.GetExtendedClient();
    }

    public void Dispose()
    {
        gitLabClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetProjectJobs_ForProject19564_Success()
    {
        try
        {
            const int projectId = 19564;
            const int pageSize = 10;

            var firstPageJobs = await gitLabClient.GetProjectJobsAsync(projectId, null, 1, pageSize);

            Assert.NotNull(firstPageJobs);
            Assert.NotNull(firstPageJobs.Result);
            Assert.Equal(pageSize, firstPageJobs.Result.Count);

            logger.LogInformation($"Retrieved {firstPageJobs.Result.Count} jobs from the first page of project {projectId}");

            if (!string.IsNullOrEmpty(firstPageJobs.NextPageLink))
            {
                logger.LogInformation("Next page is available. Fetching...");
                var secondPageJobs = await gitLabClient.GetProjectJobsAsync(projectId, null, 2, pageSize);

                Assert.NotNull(secondPageJobs);
                Assert.NotNull(secondPageJobs.Result);
                Assert.True(secondPageJobs.Result.Count <= pageSize);
                logger.LogInformation($"Retrieved {secondPageJobs.Result.Count} jobs from the second page of project {projectId}");
            }
            else
            {
                logger.LogInformation("No next page available.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving jobs for project 19564");
            throw;
        }
    }

    [Fact]
    public async Task GetAllProjectJobs_ForProject19564_Success()
    {
        try
        {
            const int projectId = 19564;
            var allJobs = new List<GitLabJob>();
            await foreach (var job in gitLabClient.GetAllProjectJobsAsync(projectId))
            {
                allJobs.Add(job);
            }

            Assert.NotNull(allJobs);
            logger.LogInformation($"Retrieved a total of {allJobs.Count} jobs from all pages for project {projectId}");

            foreach (var job in allJobs.Take(3))
            {
                logger.LogInformation($"Job ID: {job.Id}, Name: {job.Name}, Status: {job.Status}");
            }

            // Print info about status distribution
            var statusCounts = allJobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count);

            logger.LogInformation("Job status distribution:");
            foreach (var statusCount in statusCounts)
            {
                logger.LogInformation($"  {statusCount.Status}: {statusCount.Count} jobs");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all jobs for project 19564");
            throw;
        }
    }

    [Fact]
    public async Task CompareJobsRetrieval_SingleRequestVsPagination_ShouldMatchIds()
    {
        try
        {
            const int projectId = 19564;
            const int singleRequestLimit = 100;
            const int paginationLimit = 20;

            // Получаем первые 100 заданий одним запросом
            var singleRequestJobs = await gitLabClient.GetProjectJobsAsync(projectId, null, 1, singleRequestLimit);

            Assert.NotNull(singleRequestJobs);
            Assert.NotNull(singleRequestJobs.Result);

            logger.LogInformation($"Retrieved {singleRequestJobs.Result.Count} jobs with single request");

            // Получаем те же задания с помощью пагинации
            var paginatedJobs = new List<GitLabJob>();
            await foreach (var job in gitLabClient.GetAllProjectJobsAsync(projectId, null, paginationLimit))
            {
                paginatedJobs.Add(job);
                // Останавливаемся, когда достигнем количества запрошенных элементов в первом запросе
                if (paginatedJobs.Count >= singleRequestJobs.Result.Count)
                    break;
            }

            logger.LogInformation($"Retrieved {paginatedJobs.Count} jobs with pagination");

            // Проверяем, что количества элементов совпадают
            Assert.Equal(singleRequestJobs.Result.Count, paginatedJobs.Count);

            // Получаем список ID из обоих запросов и сравниваем
            var singleRequestIds = singleRequestJobs.Result.Select(j => j.Id).OrderBy(id => id).ToList();
            var paginatedIds = paginatedJobs.Select(j => j.Id).OrderBy(id => id).ToList();

            Assert.Equal(singleRequestIds, paginatedIds);

            // Проверка содержимого и структуры элементов
            for (int i = 0; i < Math.Min(5, singleRequestJobs.Result.Count); i++)
            {
                var singleRequestJob = singleRequestJobs.Result.FirstOrDefault(j => j.Id == paginatedIds[i]);
                var paginatedJob = paginatedJobs.FirstOrDefault(j => j.Id == paginatedIds[i]);

                Assert.NotNull(singleRequestJob);
                Assert.NotNull(paginatedJob);

                if (singleRequestJob != null && paginatedJob != null)
                {
                    Assert.Equal(singleRequestJob.Name, paginatedJob.Name);
                    Assert.Equal(singleRequestJob.Status, paginatedJob.Status);
                    Assert.Equal(singleRequestJob.CreatedAt, paginatedJob.CreatedAt);
                }
            }

            logger.LogInformation("Both methods returned identical job sets with matching IDs");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error comparing job retrieval methods");
            throw;
        }
    }

    [Fact]
    public async Task GetJob_ForSpecificJobId_MatchesExpectedJson()
    {
        const int projectId = 19564;
        const long jobId = 37872976;

        var job = await gitLabClient.GetJobAsync(projectId, jobId);

        Assert.NotNull(job);
        logger.LogInformation($"Retrieved job with ID: {job.Id}");

        // Проверяем основные поля задачи
        Assert.Equal(37872976, job.Id);
        Assert.Equal(JobStatus.Success, job.Status);
        Assert.Equal("build", job.Stage);
        Assert.Equal("Build", job.Name);
        Assert.Equal("ci-update", job.Ref);
        Assert.False(job.Tag);
        Assert.Null(job.Coverage);
        Assert.False(job.AllowFailure);
        Assert.Equal(DateTime.Parse("2025-04-03T13:17:00.156+05:00"), job.CreatedAt);
        Assert.Equal(DateTime.Parse("2025-04-03T13:17:05.920+05:00"), job.StartedAt);
        Assert.Equal(DateTime.Parse("2025-04-03T13:19:34.385+05:00"), job.FinishedAt);
        Assert.Null(job.ErasedAt);
        Assert.NotNull(job.Duration);
        Assert.NotNull(job.QueuedDuration);
        Assert.Equal(148.464903, job.Duration!.Value, 4);
        Assert.Equal(5.324964, job.QueuedDuration!.Value, 4);

        // Проверяем поля пользователя
        Assert.NotNull(job.User);
        Assert.Equal(4381, job.User.Id);
        Assert.Equal("mnoskov", job.User.Username);
        Assert.Equal("Носков Михаил Юрьевич", job.User.Name);
        Assert.Equal("active", job.User.State);
        Assert.Equal("https://git.skbkontur.ru/uploads/-/system/user/avatar/4381/avatar.png", job.User.AvatarUrl);
        Assert.Equal("https://git.skbkontur.ru/mnoskov", job.User.WebUrl);

        // Проверяем поля коммита
        Assert.NotNull(job.Commit);
        Assert.Equal("d3234b69806023b3fd52e1556e1526efeef93fae", job.Commit.Id);
        Assert.Equal("d3234b69", job.Commit.ShortId);
        Assert.Equal("Лишний аргумент", job.Commit.Title);
        Assert.Equal("Лишний аргумент\n", job.Commit.Message);
        Assert.Equal("Noskov Mikhail", job.Commit.AuthorName);
        Assert.Equal("mnoskov@skbkontur.ru", job.Commit.AuthorEmail);
        Assert.Equal(DateTime.Parse("2025-04-03T13:16:57.000+05:00"), job.Commit.CreatedAt);

        // Проверяем поля пайплайна
        Assert.NotNull(job.Pipeline);
        Assert.Equal(4105431, job.Pipeline.Id);
        Assert.Equal(19564, job.Pipeline.ProjectId);
        Assert.Equal("d3234b69806023b3fd52e1556e1526efeef93fae", job.Pipeline.Sha);
        Assert.Equal("ci-update", job.Pipeline.Ref);
        Assert.Equal("success", job.Pipeline.Status);
        Assert.Equal("push", job.Pipeline.Source);
        Assert.Equal("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/pipelines/4105431", job.Pipeline.WebUrl);

        // Проверяем URL и информацию о проекте
        Assert.Equal("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/jobs/37872976", job.WebUrl);
        Assert.NotNull(job.Project);
        Assert.False(job.Project.CiJobTokenScopeEnabled);

        // Проверяем остальные поля
        Assert.Equal(DateTime.Parse("2025-04-04T13:19:32.351+05:00"), job.ArtifactsExpireAt);
        Assert.False(job.Archived);
        Assert.NotNull(job.TagList);
        Assert.Empty(job.TagList!);
    }

    [Fact]
    public async Task FindJobById_ForSpecificJob_MatchesExpectedJson()
    {
        const int projectId = 19564;
        const long expectedJobId = 37872978;

        GitLabJob? foundJob = null;

        await foreach (var job in gitLabClient.GetAllProjectJobsAsync(projectId))
        {
            if (job.Id == expectedJobId)
            {
                foundJob = job;
                break;
            }
        }

        Assert.NotNull(foundJob);

        if (foundJob != null)
        {
            logger.LogInformation($"Found job with ID: {foundJob.Id}");

            Assert.Equal(37872978, foundJob.Id);
            Assert.Equal(JobStatus.Success, foundJob.Status);
            Assert.Equal("tests", foundJob.Stage);
            Assert.Equal("Integration tests", foundJob.Name);
            Assert.Equal("ci-update", foundJob.Ref);
            Assert.False(foundJob.Tag);
            Assert.Null(foundJob.Coverage);
            Assert.False(foundJob.AllowFailure);
            Assert.Equal(DateTime.Parse("2025-04-03T13:17:00.187+05:00"), foundJob.CreatedAt);
            Assert.Equal(DateTime.Parse("2025-04-03T13:19:39.590+05:00"), foundJob.StartedAt);
            Assert.Equal(DateTime.Parse("2025-04-03T13:20:48.028+05:00"), foundJob.FinishedAt);
            Assert.Null(foundJob.ErasedAt);
            Assert.NotNull(foundJob.Duration);
            Assert.NotNull(foundJob.QueuedDuration);
            Assert.Equal(68.43821, foundJob.Duration!.Value, 4);
            Assert.Equal(5.016271, foundJob.QueuedDuration!.Value, 4);

            Assert.NotNull(foundJob.User);
            Assert.Equal(4381, foundJob.User.Id);
            Assert.Equal("mnoskov", foundJob.User.Username);
            Assert.Equal("Носков Михаил Юрьевич", foundJob.User.Name);
            Assert.Equal("active", foundJob.User.State);
            Assert.Equal("https://git.skbkontur.ru/uploads/-/system/user/avatar/4381/avatar.png", foundJob.User.AvatarUrl);
            Assert.Equal("https://git.skbkontur.ru/mnoskov", foundJob.User.WebUrl);

            Assert.NotNull(foundJob.Commit);
            Assert.Equal("d3234b69806023b3fd52e1556e1526efeef93fae", foundJob.Commit.Id);
            Assert.Equal("d3234b69", foundJob.Commit.ShortId);
            Assert.Equal("Лишний аргумент", foundJob.Commit.Title);
            Assert.Equal("Лишний аргумент\n", foundJob.Commit.Message);
            Assert.Equal("Noskov Mikhail", foundJob.Commit.AuthorName);
            Assert.Equal("mnoskov@skbkontur.ru", foundJob.Commit.AuthorEmail);

            Assert.NotNull(foundJob.Pipeline);
            Assert.Equal(4105431, foundJob.Pipeline.Id);
            Assert.Equal(19564, foundJob.Pipeline.ProjectId);
            Assert.Equal("d3234b69806023b3fd52e1556e1526efeef93fae", foundJob.Pipeline.Sha);
            Assert.Equal("ci-update", foundJob.Pipeline.Ref);
            Assert.Equal("success", foundJob.Pipeline.Status);
            Assert.Equal("push", foundJob.Pipeline.Source);
            Assert.Equal("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/pipelines/4105431", foundJob.Pipeline.WebUrl);

            Assert.Equal("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/jobs/37872978", foundJob.WebUrl);
            Assert.NotNull(foundJob.Project);
            Assert.False(foundJob.Project.CiJobTokenScopeEnabled);

            Assert.Equal(DateTime.Parse("2025-04-04T13:20:45.601+05:00"), foundJob.ArtifactsExpireAt);
            Assert.False(foundJob.Archived);
            Assert.NotNull(foundJob.TagList);
            Assert.Empty(foundJob.TagList!);
        }
    }

    [Fact]
    public async Task GetLast600Jobs_ForProject4845_Success()
    {
        const int projectId = 4845;
        // const int projectId = 25483;
        const int maxJobsToRetrieve = 600;

        const JobScope scopes = JobScope.All &
                ~JobScope.Canceled &
                ~JobScope.Skipped &
                ~JobScope.Pending &
                ~JobScope.Running &
                ~JobScope.Created;
        var retrievedJobs = await gitLabClient.GetAllProjectJobsAsync(projectId, scopes, perPage: 100).Take(maxJobsToRetrieve).ToListAsync();

        Assert.NotNull(retrievedJobs);
        Assert.True(retrievedJobs.Count <= maxJobsToRetrieve);

        logger.LogInformation($"Retrieved {retrievedJobs.Count} jobs for project {projectId}");

        foreach (var job in retrievedJobs.Take(5))
        {
            logger.LogInformation($"Job ID: {job.Id}, Name: {job.Name}, Status: {job.Status}");
        }

        // Проверяем, что все задачи имеют валидные ID
        Assert.True(retrievedJobs.All(job => job.Id > 0));

        // Проверяем распределение статусов
        var statusCounts = retrievedJobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);

        logger.LogInformation("Job status distribution:");
        foreach (var statusCount in statusCounts)
        {
            logger.LogInformation($"  {statusCount.Status}: {statusCount.Count} jobs");
        }
    }

    private readonly GitLabExtendedClient gitLabClient;
    private readonly ILogger logger;
}
