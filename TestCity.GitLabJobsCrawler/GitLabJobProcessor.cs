using Kontur.TestAnalytics.Core;
using Kontur.TestAnalytics.Reporter.Client;
using NGitLab;
using System.Text.RegularExpressions;

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
        try
        {
            var jobClient = client.GetJobs(projectId);
            var jobTrace = await CreateTraceTextReader(jobClient, jobRunId);
            var job = await jobClient.GetAsync(jobRunId);
            var result = new GitLabJobProcessingResult
            {
                JobInfo = null,
                TestReportData = null,
            };

            var customStatusMessage = await ExtractTeamCityStatusMessage(jobTrace);

            if (job.Artifacts == null && customStatusMessage == null)
            {
                return result;
            }

            var artifactContents = jobClient.GetJobArtifacts(job.Id);
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

            if (result.JobInfo != null)
            {
                result.JobInfo.CustomStatusMessage = customStatusMessage;
            }

            return result;
        }
        finally
        {
            logger.LogInformation("Finish processing job with id: ProjectId: {ProjectId} JobId: {JobRunId}", projectId, jobRunId);
        }
    }

    private static async Task<string> ExtractTeamCityStatusMessage(TextReader jobTrace)
    {
        var pattern = @"##(team|test)city\[buildStatus text='(?<statusText>.*?)'\]";
        var regex = new Regex(pattern);

        string? line;
        string lastStatusMessage = string.Empty;

        while ((line = await jobTrace.ReadLineAsync()) != null)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                string escapedMessage = match.Groups["statusText"].Value;
                lastStatusMessage = UnescapeTeamCityMessage(escapedMessage);
            }
        }

        return lastStatusMessage;
    }

    private static string UnescapeTeamCityMessage(string message)
    {
        return message
            .Replace("|'", "'")
            .Replace("|n", "\n")
            .Replace("|r", "\r")
            .Replace("||", "|")
            .Replace("|[", "[")
            .Replace("|]", "]");
    }

    private static async Task<TextReader> CreateTraceTextReader(IJobClient jobClient, long jobRunId)
    {
        var traceFull = await jobClient.GetTraceAsync(jobRunId);
        return new StringReader(traceFull);
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
