using Kontur.TestAnalytics.Core;
using Kontur.TestAnalytics.Reporter.Client;
using NGitLab;

namespace Kontur.TestCity.GitLabJobsCrawler;

public class GitLabJobProcessor
{
    public GitLabJobProcessor(IGitLabClient client, JUnitExtractor extractor, ILogger logger)
    {
        this.logger = logger;
        this.client = client;
        this.extractor = extractor;
    }

    public async Task<GitLabJobProcessingResult> ProcessJobAsync(int projectId, long jobRunId)
    {
        logger.LogInformation("Start processing job with id: ProjectId: {ProjectId} JobId: {JobRunId}", projectId, jobRunId);

        var job = await client.GetJobs(projectId).GetAsync(jobRunId);
        var result = new GitLabJobProcessingResult
        {
            JobInfo = null,
            TestReportData = null,
        };
        if (job.Artifacts == null)
        {
            return result;
        }

        var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
        logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);

        var extractResult = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
        if (extractResult.TestReportData == null && !extractResult.HasCodeQualityReport)
        {
            logger.LogInformation("JobRunId '{JobRunId}' does not contain any tests or code quality reports. Skip uploading test runs", job.Id);
            return result;
        }

        result.TestReportData = extractResult.TestReportData;
        var refId = await client.BranchOrRef(projectId, job.Ref);
        result.JobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult, projectId.ToString());

        return result;
    }

    private readonly ILogger logger;
    private readonly IGitLabClient client;
    private readonly JUnitExtractor extractor;
}

public class GitLabJobProcessingResult
{
    public required TestReportData? TestReportData { get; set; }
    public required FullJobInfo? JobInfo { get; set; }
}
