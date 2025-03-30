using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public static class GitLabHelpers
{
    public static FullJobInfo GetFullJobInfo(NGitLab.Models.Job job, string refId, ArtifactsContentsInfo testCount, string projectId)
    {
        var endDateTime = DateTime.Now;

        var shortJobInfo = new JobRunInfo()
        {
            JobUrl = job.WebUrl,
            JobId = job.Name,
            PipelineId = job.Pipeline.Id.ToString(),
            JobRunId = job.Id.ToString(),
            BranchName = refId,

            // TODO Надо добавить в модельки NGitLab поля
            AgentName = job.Runner?.Description ?? $"agent_${job.Runner?.Id ?? 0}",
            AgentOSName = "Unknown",
        };
        return new FullJobInfo
        {
            JobUrl = shortJobInfo.JobUrl,
            JobId = shortJobInfo.JobId,
            PipelineId = shortJobInfo.PipelineId,
            JobRunId = shortJobInfo.JobRunId,
            BranchName = shortJobInfo.BranchName,
            AgentName = shortJobInfo.AgentName,
            AgentOSName = shortJobInfo.AgentOSName,
            State = GetJobStatus(job.Status),
            StartDateTime = job.StartedAt,
            EndDateTime = endDateTime,
            Duration = (int)Math.Ceiling(job.Duration ?? 0),
            Triggered = job.User.Email,

            // TODO job.Pipeline.Source -- это поляна есть в api но в модель её не протащили , надо сгонять к чувакам и сделать им PR
            PipelineSource = "push",
            CommitSha = job.Commit.Id.ToString(),
            CommitMessage = job.Commit.Message,
            CommitAuthor = job.Commit.AuthorName,
            ProjectId = projectId,
            CustomStatusMessage = string.Empty,
            TotalTestsCount = testCount.TestReportData?.Counters.Total ?? 0,
            SuccessTestsCount = testCount.TestReportData?.Counters.Success ?? 0,
            FailedTestsCount = testCount.TestReportData?.Counters.Failed ?? 0,
            SkippedTestsCount = testCount.TestReportData?.Counters.Skipped ?? 0,
            HasCodeQualityReport = testCount.HasCodeQualityReport,
        };
    }

    private static JobStatus GetJobStatus(NGitLab.JobStatus gitlabStatus)
    {
        if (gitlabStatus == NGitLab.JobStatus.Success)
        {
            return JobStatus.Success;
        }
        else if (gitlabStatus == NGitLab.JobStatus.Canceled)
        {
            return JobStatus.Canceled;
        }
        else if (gitlabStatus == NGitLab.JobStatus.Failed)
        {
            return JobStatus.Failed;
        }

        return JobStatus.Failed;
    }
}
