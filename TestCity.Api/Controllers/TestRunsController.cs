using Microsoft.AspNetCore.Mvc;
using TestCity.Api.Exceptions;
using TestCity.Api.Models;
using TestCity.Api.Models.Dashboard;
using TestCity.Core.GitLab;
using TestCity.Core.GitlabProjects;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/groups-v2/{groupPath1}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}/{groupPath4}")]
[Route("api/groups-v2/{groupPath1}/{groupPath2}/{groupPath3}/{groupPath4}/{groupPath5}")]
public class TestRunsController(GitLabPathResolver gitLabPathResolver, TestCityDatabase database, GitLabSettings gitLabSettings) : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<EntityNodeDto>> GetEntity()
    {
        var groupOrProjectPath = await ResolveGroupOrProjectPathFromContext();
        var currentNode = groupOrProjectPath.ResolvedEntity;

        if (currentNode is GitLabProject project)
        {
            return Ok(new ProjectEntityNodeDto
            {
                Id = project.Id,
                Title = project.Title,
                AvatarUrl = project.AvatarUrl
            });
        }
        else if (currentNode is GitLabGroup group)
        {
            return Ok(BuildGroupEntityNode(group));
        }

        throw new InvalidOperationException("Unknown node type");
    }

    private GroupEntityNodeDto BuildGroupEntityNode(GitLabGroup group)
    {
        var projects = new List<ProjectEntityNodeDto>();
        var groups = new List<GroupEntityNodeDto>();

        foreach (var childGroup in group.Groups)
        {
            groups.Add(BuildGroupEntityNode(childGroup));
        }

        foreach (var childProject in group.Projects)
        {
            projects.Add(new ProjectEntityNodeDto
            {
                Id = childProject.Id,
                Title = childProject.Title,
                AvatarUrl = childProject.AvatarUrl
            });
        }

        return new GroupEntityNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            AvatarUrl = group.AvatarUrl,
            Groups = groups,
            Projects = projects
        };
    }

    [HttpGet("branches")]
    public async Task<ActionResult<string[]>> FindAllBranches([FromQuery] string? jobId = null)
    {
        var projects = await ResolveProjectsFromContext();
        return Ok(await database.FindBranches([.. projects.Select(p => p.Id)], jobId));
    }

    [HttpGet("jobs/{jobId}/flaky-tests-count")]
    public async Task<ActionResult<int>> GetFlakyTestsCount(string jobId, [FromQuery] double flipRateThreshold = 0.1)
    {
        jobId = Uri.UnescapeDataString(jobId);
        var project = await GetProjectFromContext();

        var count = await database.GetFlakyTestsCount(project.Id, jobId, flipRateThreshold);
        return Ok(count);
    }

    [HttpGet("jobs/{jobId}/flaky-tests")]
    public async Task<ActionResult<List<FlakyTestDto>>> GetFlakyTests(string jobId, [FromQuery] double flipRateThreshold = 0.1, [FromQuery] int page = 0)
    {
        var project = await GetProjectFromContext();

        var tests = await database.GetFlakyTests(project.Id, jobId, 50, page * 50, flipRateThreshold);
        return Ok(tests.Select(MapToFlakyTestDto).ToList());
    }

    [HttpGet("jobs/{jobId}/flaky-tests-names")]
    public async Task<ActionResult<string[]>> GetFlakyTestsNames(string jobId, [FromQuery] double flipRateThreshold = 0.1)
    {
        var project = await GetProjectFromContext();

        var count = await database.GetFlakyTestNames(project.Id, jobId, flipRateThreshold);
        return Ok(count);
    }

    [HttpGet("tests/{testId}/stats")]
    public async Task<ActionResult<List<TestStatsDto>>> GetTestStats(string testId, [FromQuery] string? branchName = null)
    {
        testId = Uri.UnescapeDataString(testId);
        var project = await GetProjectFromContext();

        var jobIds = (await database.FindAllJobs([project.Id])).Select(j => j.JobId).ToArray();
        var stats = await database.GetTestStats(testId, jobIds, branchName);
        return Ok(stats.Select(MapToTestStatsDto).ToList());
    }

    [HttpGet("tests/{testId}/runs")]
    public async Task<ActionResult<List<TestPerJobRunDto>>> GetTestRuns(string testId, [FromQuery] string? branchName = null, [FromQuery] int page = 0)
    {
        testId = Uri.UnescapeDataString(testId);
        var project = await GetProjectFromContext();

        var jobIds = (await database.FindAllJobs([project.Id])).Select(j => j.JobId).ToArray();
        var runs = await database.GetTestRuns(testId, jobIds, branchName, page);
        return Ok(runs.Select(MapToTestPerJobRunDto).ToList());
    }

    [HttpGet("tests/{testId}/run-count")]
    public async Task<ActionResult<int>> GetTestRunCount(string testId, [FromQuery] string? branchName = null)
    {
        testId = Uri.UnescapeDataString(testId);
        var project = await GetProjectFromContext();

        var jobIds = (await database.FindAllJobs([project.Id])).Select(j => j.JobId).ToArray();
        var count = await database.GetTestRunCount(testId, jobIds, branchName);
        return Ok(count);
    }

    [HttpGet("jobs/{jobId}/runs")]
    public async Task<ActionResult<List<JobRunDto>>> GetJobRuns(string jobId, [FromQuery] string? branchName = null, [FromQuery] int page = 0)
    {
        var project = await GetProjectFromContext();

        var jobRuns = await database.FindAllJobsRunsPerJobId(project.Id, jobId, branchName, page);
        return Ok(jobRuns.Select(MapToJobRunDto).ToList());
    }

    [HttpGet("jobs/{jobId}/runs/{jobRunId}")]
    public async Task<ActionResult<JobInfoDto>> GetJobRun(string jobId, string jobRunId)
    {
        var project = await GetProjectFromContext();

        var jobInfo = await database.GetJobInfo(project.Id, jobId, jobRunId);
        if (jobInfo == null)
        {
            return NotFound($"Job run with id {jobRunId} not found");
        }

        return Ok(MapToJobInfoDto(jobInfo));
    }

    [HttpGet("jobs/{jobId}/runs/{jobRunId}/tests")]
    public async Task<ActionResult<List<TestRunDto>>> GetTestList(
        string jobId,
        string jobRunId,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? testIdQuery = null,
        [FromQuery] string? testStateFilter = null,
        [FromQuery] int itemsPerPage = 100,
        [FromQuery] int page = 0)
    {
        var project = await GetProjectFromContext();

        var testRuns = await database.GetTestList(
            project.Id,
            jobId,
            [jobRunId],
            sortField,
            sortDirection,
            testIdQuery,
            testStateFilter,
            itemsPerPage,
            page);

        return Ok(testRuns.Select(MapToTestRunDto).ToList());
    }

    [HttpGet("jobs/{jobId}/runs/{jobRunId}/tests-stats")]
    public async Task<ActionResult<TestListStatsDto>> GetTestsStats(
        string jobId,
        string jobRunId,
        [FromQuery] string? testIdQuery = null,
        [FromQuery] string? testStateFilter = null)
    {
        var project = await GetProjectFromContext();

        var stats = await database.GetTestListStats(
            project.Id,
            jobId,
            [jobRunId],
            testIdQuery,
            testStateFilter);

        if (stats == null)
        {
            return NotFound($"Test stats not found for job run {jobRunId}");
        }

        return Ok(MapToTestListStatsDto(stats));
    }

    [HttpGet("jobs/{jobId}/runs/{jobRunId}/tests/{testId}/output")]
    public async Task<ActionResult<TestOutputDto>> GetTestOutput(string jobId, string jobRunId, string testId)
    {
        testId = Uri.UnescapeDataString(testId);
        var project = await GetProjectFromContext();

        var testOutput = await database.GetTestOutput(jobId, testId, [jobRunId]);
        if (testOutput == null)
        {
            return NotFound($"Test output not found for test {testId}");
        }

        return Ok(MapToTestOutputDto(testOutput));
    }

    [HttpGet("jobs/{jobId}/runs/{jobRunId}/tests/download-csv")]
    public async Task<IActionResult> DownloadTestsAsCsv(string jobId, string jobRunId)
    {
        var project = await GetProjectFromContext();

        var testRuns = await database.GetTestListForCsv(jobId, [jobRunId]);

        var csv = ConvertTestsToCsv(testRuns);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

        return File(bytes, "text/csv", $"{jobRunId}.csv");
    }

    private static string ConvertTestsToCsv(TestRunForCsvQueryResult[] testRuns)
    {
        var lines = new List<string>
        {
            "Order#,Test Name,Status,Duration(ms)"
        };

        foreach (var test in testRuns)
        {
            var rowNumber = EscapeCsvField(test.RowNumber.ToString());
            var testId = EscapeCsvField(test.TestId);
            var state = EscapeCsvField(test.State);
            var duration = EscapeCsvField(test.Duration.ToString());

            lines.Add($"{rowNumber},{testId},{state},{duration}");
        }

        return string.Join("\n", lines);
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardNodeDto>> GetDashboardData([FromQuery] string? branchName = null)
    {
        var groupOrProjectPath = await ResolveGroupOrProjectPathFromContext();
        var projects = await ResolveProjectsFromContext();
        var projectIds = projects.Select(p => p.Id).ToArray();

        var allJobs = await database.FindAllJobs(projectIds);
        var inProgressJobRuns = await database.FindAllJobsRunsInProgress(projectIds, branchName);
        var allJobRuns = await database.FindAllJobsRuns(projectIds, branchName);

        var result = BuildDashboardData(groupOrProjectPath, allJobs, inProgressJobRuns, allJobRuns);
        return Ok(result);
    }

    private DashboardNodeDto BuildDashboardData(
        ResolveGroupOrProjectPathResult groupOrProjectPath,
        JobIdWithParentProject[] allJobs,
        JobRunQueryResult[] inProgressJobRuns,
        JobRunQueryResult[] allJobRuns)
    {
        var currentNode = groupOrProjectPath.ResolvedEntity;

        if (currentNode is GitLabProject project)
        {
            return BuildProjectDashboardData(groupOrProjectPath.PathSlug, project, allJobs, inProgressJobRuns, allJobRuns);
        }
        else if (currentNode is GitLabGroup group)
        {
            return BuildGroupDashboardData(groupOrProjectPath.PathSlug, group, allJobs, inProgressJobRuns, allJobRuns);
        }

        throw new InvalidOperationException("Unknown node type");
    }

    private ProjectDashboardNodeDto BuildProjectDashboardData(
        GitLabEntity[] groupOrProjectPath,
        GitLabProject project,
        JobIdWithParentProject[] allJobs,
        JobRunQueryResult[] inProgressJobRuns,
        JobRunQueryResult[] allJobRuns)
    {
        var projectJobs = allJobs.Where(j => j.ProjectId == project.Id).ToArray();
        var jobsWithRuns = projectJobs.Select(job =>
        {
            var jobRuns = inProgressJobRuns.Concat(allJobRuns)
                .Where(x => x.JobId == job.JobId && x.ProjectId == job.ProjectId)
                .Select(MapToJobRunDto)
                .ToList();

            return new JobDashboardInfoDto
            {
                JobId = job.JobId,
                Runs = jobRuns
            };
        }).ToList();

        return new ProjectDashboardNodeDto
        {
            Id = project.Id,
            Title = project.Title,
            AvatarUrl = project.AvatarUrl,
            Type = "project",
            Link = $"/api/groups-v2/{string.Join("/", groupOrProjectPath.Select(GetNodeId))}",
            GitLabLink = new Uri(gitLabSettings.Url, string.Join("/", groupOrProjectPath.Select(GetNodePathItem))).ToString(),
            FullPathSlug = groupOrProjectPath.Select(CreatePathSlugItem).ToList(),
            Jobs = jobsWithRuns
        };
    }

    private GroupDashboardNodeDto BuildGroupDashboardData(
        GitLabEntity[] groupOrProjectPath,
        GitLabGroup group,
        JobIdWithParentProject[] allJobs,
        JobRunQueryResult[] inProgressJobRuns,
        JobRunQueryResult[] allJobRuns)
    {
        var children = new List<DashboardNodeDto>();

        foreach (var childProject in group.Projects)
        {
            var childPath = new List<GitLabEntity>(groupOrProjectPath) { childProject }.ToArray();
            children.Add(BuildProjectDashboardData(childPath, childProject, allJobs, inProgressJobRuns, allJobRuns));
        }

        foreach (var childGroup in group.Groups)
        {
            var childPath = new List<GitLabEntity>(groupOrProjectPath) { childGroup }.ToArray();
            children.Add(BuildGroupDashboardData(childPath, childGroup, allJobs, inProgressJobRuns, allJobRuns));
        }

        return new GroupDashboardNodeDto
        {
            Id = group.Id,
            Title = group.Title,
            AvatarUrl = group.AvatarUrl,
            Type = "group",
            Link = $"/api/groups-v2/{string.Join("/", groupOrProjectPath.Select(GetNodeId))}",
            FullPathSlug = groupOrProjectPath.Select(CreatePathSlugItem).ToList(),
            Children = children
        };
    }

    private static string GetNodeId(GitLabEntity node)
    {
        return node.Id;
    }

    private static string GetNodePathItem(GitLabEntity node)
    {
        return node.Title;
    }

    private static GroupOrProjectPathSlugItemDto CreatePathSlugItem(GitLabEntity node)
    {
        return new GroupOrProjectPathSlugItemDto { Id = node.Id, Title = node.Title, AvatarUrl = node.AvatarUrl };
    }

    private async Task<ResolveGroupOrProjectPathResult> ResolveGroupOrProjectPathFromContext()
    {
        return await gitLabPathResolver.ResolveGroupOrProjectPath(ExtractGroupPathFromRoute());
    }

    private string[] ExtractGroupPathFromRoute()
    {
        return RouteData.Values
            .Where(kv => kv.Key.StartsWith("groupPath"))
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value?.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToArray();
    }

    private async Task<GitLabProject> GetProjectFromContext()
    {
        var groupOrProjectPath = await ResolveGroupOrProjectPathFromContext();
        var currentNode = groupOrProjectPath.ResolvedEntity;

        if (currentNode is not GitLabProject project)
        {
            throw new HttpStatusException(400, "This operation can only be performed for a specific project");
        }

        return project;
    }

    private async Task<GitLabProject[]> ResolveProjectsFromContext()
    {
        return await gitLabPathResolver.ResolveProjects(ExtractGroupPathFromRoute());
    }

    private static JobRunDto MapToJobRunDto(JobRunQueryResult source)
    {
        return new JobRunDto
        {
            JobId = source.JobId,
            JobRunId = source.JobRunId,
            BranchName = source.BranchName,
            AgentName = source.AgentName,
            StartDateTime = source.StartDateTime,
            TotalTestsCount = source.TotalTestsCount,
            AgentOSName = source.AgentOSName,
            Duration = source.Duration,
            SuccessTestsCount = source.SuccessTestsCount,
            SkippedTestsCount = source.SkippedTestsCount,
            FailedTestsCount = source.FailedTestsCount,
            State = source.State,
            CustomStatusMessage = source.CustomStatusMessage,
            JobUrl = source.JobUrl,
            ProjectId = source.ProjectId,
            HasCodeQualityReport = source.HasCodeQualityReport,
            ChangesSinceLastRun = source.ChangesSinceLastRun.Select(c => new CommitParentsChangesEntryDto
            {
                ParentCommitSha = c.ParentCommitSha,
                Depth = c.Depth,
                AuthorName = c.AuthorName,
                AuthorEmail = c.AuthorEmail,
                MessagePreview = c.MessagePreview
            }).ToList(),
            TotalCoveredCommitCount = source.TotalCoveredCommitCount
        };
    }

    private static JobInfoDto MapToJobInfoDto(JobInfoQueryResult source)
    {
        return new JobInfoDto
        {
            JobId = source.JobId,
            JobRunId = source.JobRunId,
            BranchName = source.BranchName,
            AgentName = source.AgentName,
            StartDateTime = source.StartDateTime,
            EndDateTime = source.EndDateTime,
            TotalTestsCount = source.TotalTestsCount,
            AgentOSName = source.AgentOSName,
            Duration = source.Duration,
            SuccessTestsCount = source.SuccessTestsCount,
            SkippedTestsCount = source.SkippedTestsCount,
            FailedTestsCount = source.FailedTestsCount,
            State = source.State,
            CustomStatusMessage = source.CustomStatusMessage,
            JobUrl = source.JobUrl,
            ProjectId = source.ProjectId,
            PipelineSource = source.PipelineSource,
            Triggered = source.Triggered,
            HasCodeQualityReport = source.HasCodeQualityReport,
            ChangesSinceLastRun = source.ChangesSinceLastRun.Select(c => new CommitParentsChangesEntryDto
            {
                ParentCommitSha = c.Item1,
                Depth = c.Item2,
                AuthorName = c.Item3,
                AuthorEmail = c.Item4,
                MessagePreview = c.Item5
            }).ToList(),
            TotalCoveredCommitCount = source.TotalCoveredCommitCount,
            PipelineId = source.PipelineId,
            CommitSha = source.CommitSha,
            CommitMessage = source.CommitMessage,
            CommitAuthor = source.CommitAuthor
        };
    }

    private static TestRunDto MapToTestRunDto(TestRunQueryResult source)
    {
        return new TestRunDto
        {
            FinalState = source.FinalState,
            TestId = source.TestId,
            AvgDuration = source.AvgDuration,
            MinDuration = source.MinDuration,
            MaxDuration = source.MaxDuration,
            JobId = source.JobId,
            AllStates = source.AllStates,
            StartDateTime = source.StartDateTime,
            TotalRuns = source.TotalRuns
        };
    }

    private static TestOutputDto MapToTestOutputDto(TestOutput source)
    {
        return new TestOutputDto
        {
            FailureMessage = source.FailureMessage,
            FailureOutput = source.FailureOutput,
            SystemOutput = source.SystemOutput
        };
    }

    private static FlakyTestDto MapToFlakyTestDto(FlakyTestQueryResult source)
    {
        return new FlakyTestDto
        {
            ProjectId = source.ProjectId,
            JobId = source.JobId,
            TestId = source.TestId,
            LastRunDate = source.LastRunDate,
            RunCount = source.RunCount,
            FailCount = source.FailCount,
            FlipCount = source.FlipCount,
            UpdatedAt = source.UpdatedAt,
            FlipRate = source.FlipRate
        };
    }

    private static TestStatsDto MapToTestStatsDto(TestStatsQueryResult source)
    {
        return new TestStatsDto
        {
            State = source.State,
            Duration = source.Duration,
            StartDateTime = source.StartDateTime
        };
    }

    private static TestPerJobRunDto MapToTestPerJobRunDto(TestPerJobRunQueryResult source)
    {
        return new TestPerJobRunDto
        {
            JobId = source.JobId,
            JobRunId = source.JobRunId,
            BranchName = source.BranchName,
            State = source.State,
            Duration = source.Duration,
            StartDateTime = source.StartDateTime,
            JobUrl = source.JobUrl,
            CustomStatusMessage = source.CustomStatusMessage
        };
    }

    private static TestListStatsDto MapToTestListStatsDto(TestListStats source)
    {
        return new TestListStatsDto
        {
            TotalTestsCount = source.TotalTestsCount,
            SuccessTestsCount = source.SuccessTestsCount,
            SkippedTestsCount = source.SkippedTestsCount,
            FailedTestsCount = source.FailedTestsCount
        };
    }
}
