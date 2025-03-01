using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;

namespace Kontur.TestAnalytics.Reporter.Client.Impl;

internal class JobInfoUploaderInternal
{
    private readonly ClickHouseConnection connection;

    public JobInfoUploaderInternal(ClickHouseConnection connection)
    {
        this.connection = connection;
    }

    public async Task UploadAsync(FullJobInfo info)
    {
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "JobInfo",
            BatchSize = 1,
            ColumnNames = Fields,
        };
        await bulkCopyInterface.InitAsync();

        await bulkCopyInterface.WriteToServerAsync(
            [
                [
                    info.JobId,
                    info.JobRunId,
                    info.BranchName,
                    info.AgentName,
                    info.AgentOSName,
                    info.JobUrl,
                    info.State,
                    info.Duration,
                    info.StartDateTime.ToUniversalTime(),
                    info.EndDateTime.ToUniversalTime(),
                    info.Triggered,
                    info.PipelineSource,
                    info.CommitSha,
                    info.CommitMessage,
                    info.CommitAuthor,
                    info.TotalTestsCount,
                    info.SuccessTestsCount,
                    info.FailedTestsCount,
                    info.SkippedTestsCount,
                    info.ProjectId,
                    info.CustomStatusMessage,
                    info.PipelineId,
                ],
            ]);
    }

    private static readonly string[] Fields =
    {
        "JobId",
        "JobRunId",
        "BranchName",
        "AgentName",
        "AgentOSName",
        "JobUrl",
        "State",
        "Duration",
        "StartDateTime",
        "EndDateTime",
        "Triggered",
        "PipelineSource",
        "CommitSha",
        "CommitMessage",
        "CommitAuthor",
        "TotalTestsCount",
        "SuccessTestsCount",
        "FailedTestsCount",
        "SkippedTestsCount",
        "ProjectId",
        "CustomStatusMessage",
        "PipelineId",
    };
}
