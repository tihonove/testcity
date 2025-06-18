using System.Text.RegularExpressions;
using TestCity.Core.GitLab;
using TestCity.Core.GitLab.Models;
using TestCity.Core.JUnit;
using TestCity.Core.Storage.DTO;
using Microsoft.Extensions.Logging;
using NGitLab;

namespace TestCity.Core.JobProcessing;

public class GitLabJobProcessor(IGitLabClient client, GitLabExtendedClient clientEx, JUnitExtractor extractor, ILogger logger)
{
    public async Task<GitLabJobProcessingResult> ProcessJobAsync(long projectId, long jobRunId, GitLabJob? job)
    {
        logger.LogInformation("Start processing job with id: ProjectId: {ProjectId} JobId: {JobRunId}", projectId, jobRunId);
        try
        {
            var jobClient = client.GetJobs(projectId);
            job ??= await clientEx.GetJobAsync(projectId, jobRunId);

            const long maxArtifactSize = 500 * 1024 * 1024; // 500 MB
            var hasArtifacts = job.Artifacts?.Count > 0;
            var size = job.Artifacts?.FirstOrDefault(x => x.Filename == "artifacts.zip")?.Size;
            var artifactContents = hasArtifacts && size is < maxArtifactSize  ? jobClient.GetJobArtifactsOrNull(job.Id) : null;

            if (size > maxArtifactSize)
            {
                logger.LogInformation("Artifacts to large. {artifactSize}. {JobId}", size, job.Id);
            }
            else if (artifactContents is null)
            {
                logger.LogInformation("Artifacts does not exist for id: {JobId}", job.Id);
            }
            else
            {
                logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents.Length);
            }

            var extractResult = artifactContents != null ? extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents) : null;
            if (extractResult?.TestReportData == null && !(extractResult?.HasCodeQualityReport ?? false))
            {
                logger.LogInformation("JobRunId '{JobRunId}' does not contain any tests or code quality reports. Skip uploading test runs", job.Id);
            }

            var refId = await client.BranchOrRef(projectId, job.Ref);
            var jobInfo = GitLabHelpers.GetFullJobInfo(job, refId, extractResult, projectId.ToString());
            var result = new GitLabJobProcessingResult
            {
                JobInfo = jobInfo,
                TestReportData = extractResult?.TestReportData
            };

            if (result.JobInfo != null)
            {
                result.JobInfo.CustomStatusMessage = await ExtractTeamCityStatusMessage(jobClient, jobRunId);
            }

            return result;
        }
        finally
        {
            logger.LogInformation("Finish processing job with id: ProjectId: {ProjectId} JobId: {JobRunId}", projectId, jobRunId);
        }
    }

    private static async Task<string?> ExtractTeamCityStatusMessage(IJobClient jobClient, long jobRunId)
    {
        const string pattern = @"##(team|test)city\[buildStatus text='(?<statusText>.*?)'\]";
        var regex = new Regex(pattern);

        string? lastStatusMessage = null;

        using var jobTrace = await CreateTraceTextReader(jobClient, jobRunId);
        while (await jobTrace.ReadLineAsync() is { } line)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var escapedMessage = match.Groups["statusText"].Value;
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

    private readonly ILogger logger = logger;
    private readonly IGitLabClient client = client;
    private readonly GitLabExtendedClient clientEx = clientEx;
    private readonly JUnitExtractor extractor = extractor;
}

public class GitLabJobProcessingResult
{
    public required TestReportData? TestReportData { get; set; }
    public required FullJobInfo? JobInfo { get; set; }
}
