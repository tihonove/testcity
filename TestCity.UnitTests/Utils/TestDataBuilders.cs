using TestCity.Core.Storage.DTO;

namespace TestCity.UnitTests.Utils;

/// <summary>
/// Helper methods for creating test data objects with sensible defaults.
/// This reduces boilerplate in tests by providing default values for common fields.
/// </summary>
public static class TestDataBuilders
{
    public static JobRunInfo CreateJobRunInfo(
        string? jobId = null,
        string? projectId = null,
        string? branchName = null,
        string? pipelineId = null,
        string? agentName = null)
    {
        return new JobRunInfo
        {
            JobId = jobId ?? "test-job",
            JobRunId = Guid.NewGuid().ToString(),
            ProjectId = projectId ?? "test-project",
            BranchName = branchName ?? "main",
            AgentName = agentName ?? "test-agent",
            AgentOSName = "linux",
            JobUrl = "http://test.com",
            PipelineId = pipelineId ?? "pipeline-1"
        };
    }

    public static TestRun CreateTestRun(
        string? testId = null,
        TestResult result = TestResult.Success,
        long duration = 5000L,
        DateTime? startDateTime = null,
        string? failureMessage = null,
        string? failureOutput = null,
        string? systemOutput = null)
    {
        return new TestRun
        {
            TestId = testId ?? $"test-{Guid.NewGuid()}",
            TestResult = result,
            Duration = duration,
            StartDateTime = startDateTime ?? DateTime.UtcNow,
            JUnitFailureMessage = failureMessage,
            JUnitFailureOutput = failureOutput,
            JUnitSystemOutput = systemOutput
        };
    }

    public static FullJobInfo CreateFullJobInfo(
        string? jobId = null,
        string? jobRunId = null,
        string? projectId = null,
        string? pipelineId = null,
        string? branchName = null,
        JobStatus state = JobStatus.Success,
        DateTime? startDateTime = null,
        long duration = 1000,
        int totalTests = 10,
        int successTests = 10,
        int failedTests = 0,
        int skippedTests = 0,
        string? agentName = null,
        string? agentOSName = null,
        string? jobUrl = null,
        string? commitSha = null,
        string? commitMessage = null,
        string? commitAuthor = null,
        string? triggered = null,
        string? pipelineSource = null,
        string? customStatusMessage = null,
        List<CommitParentsChangesEntry>? changes = null)
    {
        var start = startDateTime ?? DateTime.UtcNow;
        var effectiveJobId = jobId ?? "job-1";
        return new FullJobInfo
        {
            JobId = effectiveJobId,
            JobRunId = jobRunId ?? Guid.NewGuid().ToString(),
            ProjectId = projectId ?? "test-project",
            PipelineId = pipelineId ?? "pipeline-1",
            BranchName = branchName ?? "main",
            AgentName = agentName ?? "agent-1",
            AgentOSName = agentOSName ?? "linux",
            JobUrl = jobUrl ?? $"http://test.com/{effectiveJobId}",
            State = state,
            Duration = duration,
            StartDateTime = start,
            EndDateTime = start.AddMilliseconds(duration),
            Triggered = triggered,
            PipelineSource = pipelineSource,
            CommitSha = commitSha,
            CommitMessage = commitMessage ?? (commitSha != null ? "Test commit" : null),
            CommitAuthor = commitAuthor ?? (commitSha != null ? "Author" : null),
            TotalTestsCount = totalTests,
            SuccessTestsCount = successTests,
            FailedTestsCount = failedTests,
            SkippedTestsCount = skippedTests,
            ChangesSinceLastRun = changes ?? new List<CommitParentsChangesEntry>(),
            CustomStatusMessage = customStatusMessage
        };
    }

    public static InProgressJobInfo CreateInProgressJobInfo(
        string? jobId = null,
        string? jobRunId = null,
        string? projectId = null,
        string? pipelineId = null,
        string? branchName = null,
        DateTime? startDateTime = null,
        string? agentName = null,
        string? agentOSName = null,
        string? jobUrl = null,
        List<CommitParentsChangesEntry>? changes = null)
    {
        var effectiveJobId = jobId ?? "in-progress-job";
        return new InProgressJobInfo
        {
            JobId = effectiveJobId,
            JobRunId = jobRunId ?? Guid.NewGuid().ToString(),
            ProjectId = projectId ?? "test-project",
            PipelineId = pipelineId ?? "pipeline-1",
            BranchName = branchName ?? "main",
            AgentName = agentName ?? "agent-1",
            AgentOSName = agentOSName ?? "linux",
            JobUrl = jobUrl ?? $"http://test.com/{effectiveJobId}",
            StartDateTime = startDateTime ?? DateTime.UtcNow,
            ChangesSinceLastRun = changes ?? new List<CommitParentsChangesEntry>()
        };
    }
}
