using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitLab.Models;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.GitLab;

[TestFixture]
public class GitLabJobsTests
{
    [SetUp]
    public void SetUp()
    {
        var provider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        gitLabClient = provider.GetExtendedClient(); ;
        logger = GlobalSetup.TestLoggerFactory.CreateLogger<GitLabJobsTests>();
    }

    [TearDown]
    public void TearDown()
    {
        gitLabClient.Dispose();
    }

    [Test]
    public async Task GetProjectJobs_ForProject19564_Success()
    {
        try
        {
            const int projectId = 19564;
            const int pageSize = 10;

            var firstPageJobs = await gitLabClient.GetProjectJobsAsync(projectId, null, 1, pageSize);

            Assert.That(firstPageJobs, Is.Not.Null);
            Assert.That(firstPageJobs.Result, Is.Not.Null);
            Assert.That(firstPageJobs.Result.Count, Is.EqualTo(pageSize));

            logger.LogInformation($"Retrieved {firstPageJobs.Result.Count} jobs from the first page of project {projectId}");

            if (!string.IsNullOrEmpty(firstPageJobs.NextPageLink))
            {
                logger.LogInformation("Next page is available. Fetching...");
                var secondPageJobs = await gitLabClient.GetProjectJobsAsync(projectId, null, 2, pageSize);

                Assert.That(secondPageJobs, Is.Not.Null);
                Assert.That(secondPageJobs.Result, Is.Not.Null);
                Assert.That(secondPageJobs.Result.Count, Is.LessThanOrEqualTo(pageSize));
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

    [Test]
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

            Assert.That(allJobs, Is.Not.Null);
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

    [Test]
    public async Task CompareJobsRetrieval_SingleRequestVsPagination_ShouldMatchIds()
    {
        try
        {
            const int projectId = 19564;
            const int singleRequestLimit = 100;
            const int paginationLimit = 20;

            // Получаем первые 100 заданий одним запросом
            var singleRequestJobs = await gitLabClient.GetProjectJobsAsync(projectId, null, 1, singleRequestLimit);

            Assert.That(singleRequestJobs, Is.Not.Null);
            Assert.That(singleRequestJobs.Result, Is.Not.Null);

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
            Assert.That(paginatedJobs.Count, Is.EqualTo(singleRequestJobs.Result.Count));

            // Получаем список ID из обоих запросов и сравниваем
            var singleRequestIds = singleRequestJobs.Result.Select(j => j.Id).OrderBy(id => id).ToList();
            var paginatedIds = paginatedJobs.Select(j => j.Id).OrderBy(id => id).ToList();

            Assert.That(paginatedIds, Is.EqualTo(singleRequestIds));

            // Проверка содержимого и структуры элементов
            for (int i = 0; i < Math.Min(5, singleRequestJobs.Result.Count); i++)
            {
                var singleRequestJob = singleRequestJobs.Result.FirstOrDefault(j => j.Id == paginatedIds[i]);
                var paginatedJob = paginatedJobs.FirstOrDefault(j => j.Id == paginatedIds[i]);

                Assert.That(singleRequestJob, Is.Not.Null);
                Assert.That(paginatedJob, Is.Not.Null);

                if (singleRequestJob != null && paginatedJob != null)
                {
                    Assert.That(paginatedJob.Name, Is.EqualTo(singleRequestJob.Name));
                    Assert.That(paginatedJob.Status, Is.EqualTo(singleRequestJob.Status));
                    Assert.That(paginatedJob.CreatedAt, Is.EqualTo(singleRequestJob.CreatedAt));
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

    [Test]
    public async Task GetJob_ForSpecificJobId_MatchesExpectedJson()
    {
        const int projectId = 19564;
        const long jobId = 37872976;

        var job = await gitLabClient.GetJobAsync(projectId, jobId);

        Assert.That(job, Is.Not.Null, $"Job with id {jobId} not found");
        logger.LogInformation($"Retrieved job with ID: {job.Id}");

        // Проверяем основные поля задачи
        Assert.That(job.Id, Is.EqualTo(37872976));
        Assert.That(job.Status, Is.EqualTo(JobStatus.Success));
        Assert.That(job.Stage, Is.EqualTo("build"));
        Assert.That(job.Name, Is.EqualTo("Build"));
        Assert.That(job.Ref, Is.EqualTo("ci-update"));
        Assert.That(job.Tag, Is.False);
        Assert.That(job.Coverage, Is.Null);
        Assert.That(job.AllowFailure, Is.False);
        Assert.That(job.CreatedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:17:00.156+05:00")));
        Assert.That(job.StartedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:17:05.920+05:00")));
        Assert.That(job.FinishedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:19:34.385+05:00")));
        Assert.That(job.ErasedAt, Is.Null);
        Assert.That(job.Duration, Is.EqualTo(148.464903).Within(0.0001));
        Assert.That(job.QueuedDuration, Is.EqualTo(5.324964).Within(0.0001));

        // Проверяем поля пользователя
        Assume.That(job.User is not null);
        Assert.That(job.User.Id, Is.EqualTo(4381));
        Assert.That(job.User.Username, Is.EqualTo("mnoskov"));
        Assert.That(job.User.Name, Is.EqualTo("Носков Михаил Юрьевич"));
        Assert.That(job.User.State, Is.EqualTo("active"));
        Assert.That(job.User.AvatarUrl, Is.EqualTo("https://git.skbkontur.ru/uploads/-/system/user/avatar/4381/avatar.png"));
        Assert.That(job.User.WebUrl, Is.EqualTo("https://git.skbkontur.ru/mnoskov"));

        // Проверяем поля коммита
        Assume.That(job.Commit is not null);
        Assert.That(job.Commit.Id, Is.EqualTo("d3234b69806023b3fd52e1556e1526efeef93fae"));
        Assert.That(job.Commit.ShortId, Is.EqualTo("d3234b69"));
        Assert.That(job.Commit.Title, Is.EqualTo("Лишний аргумент"));
        Assert.That(job.Commit.Message, Is.EqualTo("Лишний аргумент\n"));
        Assert.That(job.Commit.AuthorName, Is.EqualTo("Noskov Mikhail"));
        Assert.That(job.Commit.AuthorEmail, Is.EqualTo("mnoskov@skbkontur.ru"));
        Assert.That(job.Commit.CreatedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:16:57.000+05:00")));

        // Проверяем поля пайплайна
        Assume.That(job.Pipeline is not null);
        Assert.That(job.Pipeline.Id, Is.EqualTo(4105431));
        Assert.That(job.Pipeline.ProjectId, Is.EqualTo(19564));
        Assert.That(job.Pipeline.Sha, Is.EqualTo("d3234b69806023b3fd52e1556e1526efeef93fae"));
        Assert.That(job.Pipeline.Ref, Is.EqualTo("ci-update"));
        Assert.That(job.Pipeline.Status, Is.EqualTo("success"));
        Assert.That(job.Pipeline.Source, Is.EqualTo("push"));
        Assert.That(job.Pipeline.WebUrl, Is.EqualTo("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/pipelines/4105431"));

        // Проверяем URL и информацию о проекте
        Assert.That(job.WebUrl, Is.EqualTo("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/jobs/37872976"));
        Assume.That(job.Project is not null);
        Assert.That(job.Project.CiJobTokenScopeEnabled, Is.False);

        // Проверяем артефакты
        Assume.That(job.Artifacts is not null);
        Assert.That(job.Artifacts.Count, Is.EqualTo(3));

        var traceArtifact = job.Artifacts.FirstOrDefault(a => a.FileType == "trace");
        Assert.That(traceArtifact, Is.Not.Null);
        Assert.That(traceArtifact!.Size, Is.EqualTo(84847));
        Assert.That(traceArtifact.Filename, Is.EqualTo("job.log"));
        Assert.That(traceArtifact.FileFormat, Is.Null);

        var archiveArtifact = job.Artifacts.FirstOrDefault(a => a.FileType == "archive");
        Assert.That(archiveArtifact, Is.Not.Null);
        Assert.That(archiveArtifact!.Size, Is.EqualTo(1874711733));
        Assert.That(archiveArtifact.Filename, Is.EqualTo("artifacts.zip"));
        Assert.That(archiveArtifact.FileFormat, Is.EqualTo("zip"));

        var metadataArtifact = job.Artifacts.FirstOrDefault(a => a.FileType == "metadata");
        Assert.That(metadataArtifact, Is.Not.Null);
        Assert.That(metadataArtifact!.Size, Is.EqualTo(164330));
        Assert.That(metadataArtifact.Filename, Is.EqualTo("metadata.gz"));
        Assert.That(metadataArtifact.FileFormat, Is.EqualTo("gzip"));

        // Проверяем информацию о runner
        Assume.That(job.Runner is not null);
        Assert.That(job.Runner.Id, Is.EqualTo(12823));
        Assert.That(job.Runner.IsShared, Is.True);
        Assert.That(job.Runner.RunnerType, Is.EqualTo("instance_type"));
        Assert.That(job.Runner.Online, Is.True);
        Assert.That(job.Runner.Status, Is.EqualTo("online"));

        // Проверяем информацию о runner manager
        Assume.That(job.RunnerManager is not null);
        Assert.That(job.RunnerManager.Id, Is.EqualTo(4756));
        Assert.That(job.RunnerManager.SystemId, Is.EqualTo("r_OyyT3ZmvERTs"));
        Assert.That(job.RunnerManager.Version, Is.EqualTo("17.6.0"));
        Assert.That(job.RunnerManager.Revision, Is.EqualTo("374d34fd"));
        Assert.That(job.RunnerManager.Platform, Is.EqualTo("linux"));
        Assert.That(job.RunnerManager.Architecture, Is.EqualTo("amd64"));
        Assert.That(job.RunnerManager.Status, Is.EqualTo("online"));

        // Проверяем остальные поля
        Assert.That(job.ArtifactsExpireAt, Is.EqualTo(DateTime.Parse("2025-04-04T13:19:32.351+05:00")));
        Assert.That(job.Archived, Is.False);
        Assert.That(job.TagList, Is.Empty);
    }

    [Test]
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

        Assert.That(foundJob, Is.Not.Null, $"Job with id {expectedJobId} not found");

        if (foundJob != null)
        {
            logger.LogInformation($"Found job with ID: {foundJob.Id}");

            Assert.That(foundJob.Id, Is.EqualTo(37872978));
            Assert.That(foundJob.Status, Is.EqualTo(JobStatus.Success));
            Assert.That(foundJob.Stage, Is.EqualTo("tests"));
            Assert.That(foundJob.Name, Is.EqualTo("Integration tests"));
            Assert.That(foundJob.Ref, Is.EqualTo("ci-update"));
            Assert.That(foundJob.Tag, Is.False);
            Assert.That(foundJob.Coverage, Is.Null);
            Assert.That(foundJob.AllowFailure, Is.False);
            Assert.That(foundJob.CreatedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:17:00.187+05:00")));
            Assert.That(foundJob.StartedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:19:39.590+05:00")));
            Assert.That(foundJob.FinishedAt, Is.EqualTo(DateTime.Parse("2025-04-03T13:20:48.028+05:00")));
            Assert.That(foundJob.ErasedAt, Is.Null);
            Assert.That(foundJob.Duration, Is.EqualTo(68.43821).Within(0.0001));
            Assert.That(foundJob.QueuedDuration, Is.EqualTo(5.016271).Within(0.0001));

            Assume.That(foundJob.User is not null);
            Assert.That(foundJob.User.Id, Is.EqualTo(4381));
            Assert.That(foundJob.User.Username, Is.EqualTo("mnoskov"));
            Assert.That(foundJob.User.Name, Is.EqualTo("Носков Михаил Юрьевич"));
            Assert.That(foundJob.User.State, Is.EqualTo("active"));
            Assert.That(foundJob.User.AvatarUrl, Is.EqualTo("https://git.skbkontur.ru/uploads/-/system/user/avatar/4381/avatar.png"));
            Assert.That(foundJob.User.WebUrl, Is.EqualTo("https://git.skbkontur.ru/mnoskov"));

            Assume.That(foundJob.Commit is not null);
            Assert.That(foundJob.Commit.Id, Is.EqualTo("d3234b69806023b3fd52e1556e1526efeef93fae"));
            Assert.That(foundJob.Commit.ShortId, Is.EqualTo("d3234b69"));
            Assert.That(foundJob.Commit.Title, Is.EqualTo("Лишний аргумент"));
            Assert.That(foundJob.Commit.Message, Is.EqualTo("Лишний аргумент\n"));
            Assert.That(foundJob.Commit.AuthorName, Is.EqualTo("Noskov Mikhail"));
            Assert.That(foundJob.Commit.AuthorEmail, Is.EqualTo("mnoskov@skbkontur.ru"));

            Assume.That(foundJob.Pipeline is not null);
            Assert.That(foundJob.Pipeline.Id, Is.EqualTo(4105431));
            Assert.That(foundJob.Pipeline.ProjectId, Is.EqualTo(19564));
            Assert.That(foundJob.Pipeline.Sha, Is.EqualTo("d3234b69806023b3fd52e1556e1526efeef93fae"));
            Assert.That(foundJob.Pipeline.Ref, Is.EqualTo("ci-update"));
            Assert.That(foundJob.Pipeline.Status, Is.EqualTo("success"));
            Assert.That(foundJob.Pipeline.Source, Is.EqualTo("push"));
            Assert.That(foundJob.Pipeline.WebUrl, Is.EqualTo("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/pipelines/4105431"));

            Assert.That(foundJob.WebUrl, Is.EqualTo("https://git.skbkontur.ru/testers/fiit/fiit-big-library/-/jobs/37872978"));
            Assume.That(foundJob.Project is not null);
            Assert.That(foundJob.Project.CiJobTokenScopeEnabled, Is.False);

            Assume.That(foundJob.Artifacts is not null);
            Assert.That(foundJob.Artifacts.Count, Is.EqualTo(3));

            var traceArtifact = foundJob.Artifacts[0];
            Assert.That(traceArtifact.FileType, Is.EqualTo("trace"));
            Assert.That(traceArtifact.Size, Is.EqualTo(5996));
            Assert.That(traceArtifact.Filename, Is.EqualTo("job.log"));
            Assert.That(traceArtifact.FileFormat, Is.Null);

            var archiveArtifact = foundJob.Artifacts[1];
            Assert.That(archiveArtifact.FileType, Is.EqualTo("archive"));
            Assert.That(archiveArtifact.Size, Is.EqualTo(1431));
            Assert.That(archiveArtifact.Filename, Is.EqualTo("artifacts.zip"));
            Assert.That(archiveArtifact.FileFormat, Is.EqualTo("zip"));

            Assume.That(foundJob.Runner is not null);
            Assert.That(foundJob.Runner.Id, Is.EqualTo(12823));
            Assert.That(foundJob.Runner.IsShared, Is.True);
            Assert.That(foundJob.Runner.RunnerType, Is.EqualTo("instance_type"));
            Assert.That(foundJob.Runner.Online, Is.True);
            Assert.That(foundJob.Runner.Status, Is.EqualTo("online"));

            Assume.That(foundJob.RunnerManager is not null);
            Assert.That(foundJob.RunnerManager.Id, Is.EqualTo(4755));
            Assert.That(foundJob.RunnerManager.SystemId, Is.EqualTo("r_yZyCn21ZXJcF"));
            Assert.That(foundJob.RunnerManager.Version, Is.EqualTo("17.6.0"));
            Assert.That(foundJob.RunnerManager.Revision, Is.EqualTo("374d34fd"));
            Assert.That(foundJob.RunnerManager.Platform, Is.EqualTo("linux"));
            Assert.That(foundJob.RunnerManager.Architecture, Is.EqualTo("amd64"));

            Assert.That(foundJob.ArtifactsExpireAt, Is.EqualTo(DateTime.Parse("2025-04-04T13:20:45.601+05:00")));
            Assert.That(foundJob.Archived, Is.False);
            Assert.That(foundJob.TagList, Is.Empty);
        }
    }

    [Test]
    public async Task GetLast600Jobs_ForProject4845_Success()
    {
        const int projectId = 4845;
        // const int projectId = 25483;
        const int maxJobsToRetrieve = 600;

        const Core.GitLab.JobScope scopes = Core.GitLab.JobScope.All &
                ~Core.GitLab.JobScope.Canceled &
                ~Core.GitLab.JobScope.Skipped &
                ~Core.GitLab.JobScope.Pending &
                ~Core.GitLab.JobScope.Running &
                ~Core.GitLab.JobScope.Created;
        var retrievedJobs = await gitLabClient.GetAllProjectJobsAsync(projectId, scopes, perPage: 100).Take(maxJobsToRetrieve).ToListAsync();

        Assert.That(retrievedJobs, Is.Not.Null);
        Assert.That(retrievedJobs.Count, Is.LessThanOrEqualTo(maxJobsToRetrieve));

        logger.LogInformation($"Retrieved {retrievedJobs.Count} jobs for project {projectId}");

        foreach (var job in retrievedJobs.Take(5))
        {
            logger.LogInformation($"Job ID: {job.Id}, Name: {job.Name}, Status: {job.Status}");
        }

        // Проверяем, что все задачи имеют валидные ID
        Assert.That(retrievedJobs.All(job => job.Id > 0), Is.True, "All jobs should have valid IDs");

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

    private GitLabExtendedClient gitLabClient;
    private ILogger logger;
}
