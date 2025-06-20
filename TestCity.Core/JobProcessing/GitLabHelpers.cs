using TestCity.Core.GitLab.Models;
using TestCity.Core.JUnit;
using TestCity.Core.Storage.DTO;

namespace TestCity.Core.JobProcessing;

public static class GitLabHelpers
{
    public static FullJobInfo GetFullJobInfo(GitLabJob job, string? refId, ArtifactsContentsInfo? testCount, string projectId)
    {
        var endDateTime = DateTime.Now;

        var shortJobInfo = new JobRunInfo()
        {
            JobUrl = job.WebUrl,
            ProjectId = projectId,
            JobId = job.Name,
            PipelineId = job.Pipeline?.Id.ToString(),
            JobRunId = job.Id.ToString(),
            BranchName = refId,

            // TODO Need to add fields to the NGitLab models
            AgentName = job.Runner?.Name ?? job.Runner?.Description ?? $"agent_${job.Runner?.Id ?? 0}",
            AgentOSName = job.RunnerManager?.Platform ?? "Unknown",
        };
        return new FullJobInfo
        {
            JobUrl = shortJobInfo.JobUrl,
            JobId = shortJobInfo.JobId,
            ProjectId = shortJobInfo.ProjectId,
            PipelineId = shortJobInfo.PipelineId,
            JobRunId = shortJobInfo.JobRunId,
            BranchName = shortJobInfo.BranchName,
            AgentName = shortJobInfo.AgentName,
            AgentOSName = shortJobInfo.AgentOSName,
            State = GetJobStatus(job.Status),
            StartDateTime = job.StartedAt ?? DateTime.Now,
            EndDateTime = endDateTime,
            Duration = (int)Math.Ceiling(job.Duration ?? 0),
            Triggered = job.User?.PublicEmail,
            PipelineSource = job.Pipeline?.Source,
            CommitSha = job.Commit?.Id,
            CommitMessage = job.Commit?.Message,
            CommitAuthor = job.Commit?.AuthorName,
            CustomStatusMessage = string.Empty,
            TotalTestsCount = testCount?.TestReportData?.Counters.Total ?? 0,
            SuccessTestsCount = testCount?.TestReportData?.Counters.Success ?? 0,
            FailedTestsCount = testCount?.TestReportData?.Counters.Failed ?? 0,
            SkippedTestsCount = testCount?.TestReportData?.Counters.Skipped ?? 0,
            HasCodeQualityReport = testCount?.HasCodeQualityReport ?? false,
        };
    }

    private static Core.Storage.DTO.JobStatus GetJobStatus(Core.GitLab.Models.JobStatus gitlabStatus)
    {
        if (gitlabStatus == Core.GitLab.Models.JobStatus.Success)
        {
            return Core.Storage.DTO.JobStatus.Success;
        }
        else if (gitlabStatus == Core.GitLab.Models.JobStatus.Canceled)
        {
            return Core.Storage.DTO.JobStatus.Canceled;
        }
        else if (gitlabStatus == Core.GitLab.Models.JobStatus.Failed)
        {
            return Core.Storage.DTO.JobStatus.Failed;
        }

        return Core.Storage.DTO.JobStatus.Failed;
    }
}
