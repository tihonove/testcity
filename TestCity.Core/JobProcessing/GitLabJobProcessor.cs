using System.Text.RegularExpressions;
using Kontur.TestCity.Core.GitLab;
using Kontur.TestCity.Core.GitLab.Models;
using Kontur.TestCity.Core.JUnit;
using Kontur.TestCity.Core.Storage.DTO;
using Microsoft.Extensions.Logging;
using NGitLab;

namespace Kontur.TestCity.Core.JobProcessing;

public class GitLabJobProcessor(IGitLabClient client, GitLabExtendedClient clientEx, JUnitExtractor extractor, ILogger logger)
{
    public async Task<GitLabJobProcessingResult> ProcessJobAsync(long projectId, long jobRunId, GitLabJob? job, bool needProcessFailedJob)
    {
        logger.LogInformation("Start processing job with id: ProjectId: {ProjectId} JobId: {JobRunId}", projectId, jobRunId);
        try
        {
            var jobClient = client.GetJobs(projectId);
            job ??= await clientEx.GetJobAsync(projectId, jobRunId);
            var result = new GitLabJobProcessingResult
            {
                JobInfo = null,
                TestReportData = null,
            };

            const long maxArtifactSize = 500 * 1024 * 1024; // 500 MB
            var hasArtifacts = job.Artifacts?.Count > 0;
            var size = job.Artifacts?.FirstOrDefault(x => x.Filename == "artifacts.zip")?.Size;
            var artifactContents = hasArtifacts && size < maxArtifactSize  ? jobClient.GetJobArtifactsOrNull(job.Id) : null;
            if (artifactContents == null && !needProcessFailedJob)
            {
                if (size.HasValue && size > maxArtifactSize) {
                    logger.LogInformation("Artifacts to large. {artifactSize}. {JobId}", size, job.Id);
                } else {
                    logger.LogInformation("Artifacts does not exist for id: {JobId}", job.Id);
                }
                return result;
            }
            else
            {
                if (size.HasValue && size > maxArtifactSize) {
                    logger.LogInformation("Artifacts to large. {artifactSize}. {JobId}", size, job.Id);
                }
                if (needProcessFailedJob)
                    logger.LogInformation("Artifacts does not exist for id: {JobId}. But job must be processed", job.Id);
                else
                    logger.LogInformation("Artifact size for job with id: {JobId}. Size: {Size} bytes", job.Id, artifactContents?.Length ?? 0);
            }

            var jobTrace = await CreateTraceTextReader(jobClient, jobRunId);
            var customStatusMessage = await ExtractTeamCityStatusMessage(jobTrace);
            var extractResult = artifactContents != null ? extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents) : null;
            if (extractResult?.TestReportData == null && !(extractResult?.HasCodeQualityReport ?? false))
            {
                logger.LogInformation("JobRunId '{JobRunId}' does not contain any tests or code quality reports. Skip uploading test runs", job.Id);
                if (!needProcessFailedJob)
                    return result;
            }

            result.TestReportData = extractResult?.TestReportData;
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

    private static async Task<string?> ExtractTeamCityStatusMessage(TextReader jobTrace)
    {
        var pattern = @"##(team|test)city\[buildStatus text='(?<statusText>.*?)'\]";
        var regex = new Regex(pattern);

        string? line;
        string? lastStatusMessage = null;

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
