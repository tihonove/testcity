using TestCity.Core.Clickhouse;
using TestCity.Core.Storage;
using TestCity.Core.Storage.DTO;

namespace TestCity.Core.Storage;

public static class TestCityDatabaseExtensions
{
    public static async Task<TestOutput?> GetFailedTestOutput(
        this TestCityDatabase db,
        string jobId,
        string testId,
        string[] jobRunIds,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsync($@"
            SELECT
                JUnitFailureMessage as FailureMessage,
                JUnitFailureOutput as FailureOutput,
                JUnitSystemOutput as SystemOutput
            FROM TestRuns
            WHERE 
                TestId = {testId} 
                AND JobId = {jobId}
                AND JobRunId IN {jobRunIds}
                AND State = 'Failed'
            ORDER BY StartDateTime DESC
            LIMIT 1
        ", ct);
        return await reader.ReadSingleAsync<TestOutput>(ct);
    }

    public static async Task<JobIdWithParentProject[]> FindAllJobs(
        this TestCityDatabase db,
        string[] projectIds,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsync($@"
            SELECT DISTINCT 
                JobId, 
                ProjectId 
            FROM JobInfo 
            WHERE 
                StartDateTime >= DATE_ADD(DAY, -14, NOW()) 
                AND ProjectId IN {projectIds}
        ", ct);
        var result = await reader.ReadAllAsync<JobIdWithParentProject>(ct);
        return result.ToArray();
    }

    public static async Task<JobRunQueryResult[]> FindAllJobsRunsInProgress(
        this TestCityDatabase db,
        string[] projectIds,
        string? currentBranchName = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        var reader = await connection.ExecuteQueryAsync($@"
            SELECT
                ipji.JobId,
                ipji.JobRunId,
                ipji.BranchName,
                ipji.AgentName,
                ipji.StartDateTime,
                CAST(NULL AS Nullable(UInt32)) as TotalTestsCount,
                ipji.AgentOSName,
                CAST(NULL AS Nullable(Int64)) as Duration,
                CAST(NULL AS Nullable(UInt32)) as SuccessTestsCount,
                CAST(NULL AS Nullable(UInt32)) as SkippedTestsCount,
                CAST(NULL AS Nullable(UInt32)) as FailedTestsCount,
                'Running' as State,
                '' as CustomStatusMessage,
                ipji.JobUrl,
                ipji.ProjectId,
                0 as HasCodeQualityReport,
                length(ipji.ChangesSinceLastRun) as TotalCoveredCommitCount
            FROM InProgressJobInfo ipji
            LEFT JOIN JobInfo AS ji ON ji.JobId = ipji.JobId AND ji.JobRunId = ipji.JobRunId
            WHERE 
                ji.JobRunId = ''
                AND ipji.StartDateTime >= now() - INTERVAL 14 DAY 
                AND ipji.ProjectId IN {projectIds}
                AND ({currentBranchName} IS NULL OR ipji.BranchName = {currentBranchName})
            ORDER BY ipji.JobId, ipji.StartDateTime DESC
            LIMIT 1000
        ", ct);
        var result = await reader.ReadAllAsync<JobRunQueryResult>(ct);
        return result.ToArray();
    }

    public static async Task<JobRunQueryResult[]> FindAllJobsRuns(
        this TestCityDatabase db,
        string[] projectIds,
        string? currentBranchName = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        var reader = await connection.ExecuteQueryAsync($@"
            SELECT
                filtered.JobId,
                filtered.JobRunId,
                filtered.BranchName,
                filtered.AgentName,
                filtered.StartDateTime,
                filtered.TotalTestsCount,
                filtered.AgentOSName,
                filtered.Duration,
                filtered.SuccessTestsCount,
                filtered.SkippedTestsCount,
                filtered.FailedTestsCount,
                filtered.State,
                filtered.CustomStatusMessage,
                filtered.JobUrl,
                filtered.ProjectId,
                filtered.HasCodeQualityReport,
                arraySlice(filtered.ChangesSinceLastRun, 1, 20),
                length(filtered.ChangesSinceLastRun) as TotalCoveredCommitCount
            FROM (
                SELECT *,
                ROW_NUMBER() OVER (PARTITION BY ProjectId, JobId ORDER BY StartDateTime DESC) AS rnj
                FROM (
                    SELECT
                        *,
                        ROW_NUMBER() OVER (PARTITION BY ProjectId, JobId, BranchName ORDER BY StartDateTime DESC) AS rn
                    FROM JobInfo 
                    WHERE 
                        StartDateTime >= now() - INTERVAL 14 DAY 
                        AND ProjectId IN {projectIds}
                        AND ({currentBranchName} IS NULL OR BranchName = {currentBranchName})
                ) AS filtered_inner 
                WHERE rn = 1
            ) AS filtered
            WHERE (rnj <= 5 OR StartDateTime >= now() - INTERVAL 3 DAY)
            ORDER BY filtered.JobId, filtered.StartDateTime DESC
            LIMIT 1000
        ", ct);
        var result = await reader.ReadAllAsync<JobRunQueryResult>(ct);
        return result.ToArray();
    }

    public static async Task<string[]> FindBranches(
        this TestCityDatabase db,
        string[]? projectIds = null,
        string? jobId = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        var reader = await connection.ExecuteQueryAsync($@"
            SELECT DISTINCT 
                BranchName
            FROM JobInfo
            WHERE 
                StartDateTime >= DATE_ADD(DAY, -14, NOW()) 
                AND BranchName != '' 
                AND ({projectIds} IS NULL OR ProjectId IN {projectIds})
                AND ({jobId} IS NULL OR JobId = {jobId})
            ORDER BY StartDateTime DESC
        ", ct);

        var branches = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            branches.Add(reader.GetString(0));
        }
        return branches.ToArray();
    }

    public static async Task<PipelineRunQueryResult[]> GetPipelineRunsByProject(
        this TestCityDatabase db,
        string projectId,
        string? currentBranchName = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        var reader = await connection.ExecuteQueryAsync($@"
            SELECT 
                ProjectId as ProjectId ,
                PipelineId as PipelineId,
                BranchName as BranchName,
                MIN(StartDateTime) as StartDateTime,
                SUM(TotalTestsCount) as TotalTestsCount,
                SUM(Duration) as Duration,
                SUM(SuccessTestsCount) as SuccessTestsCount,
                SUM(SkippedTestsCount) as SkippedTestsCount,
                SUM(FailedTestsCount) as FailedTestsCount,
                MAX(State) as State,
                COUNT(JobRunId) as JobRunCount,
                arrayStringConcat(groupArrayIf(JobInfo.CustomStatusMessage, 
                JobInfo.CustomStatusMessage != ''), ', ') as CustomStatusMessage,
                arrayElement(topK(1)(CommitMessage), 1) as CommitMessage,
                arrayElement(topK(1)(CommitAuthor), 1) as CommitAuthor,
                arrayElement(topK(1)(CommitSha), 1) as CommitSha,
                0 as HasCodeQualityReport,
                arraySlice(any(ji.ChangesSinceLastRun), 1, 20),
                length(any(ji.ChangesSinceLastRun)) as TotalCoveredCommitCount
            FROM JobInfo ji
            WHERE 
                JobInfo.PipelineId <> ''
                AND JobInfo.ProjectId = {projectId}
                AND ({currentBranchName} IS NULL OR JobInfo.BranchName = {currentBranchName})
            GROUP BY 
                JobInfo.ProjectId,
                JobInfo.PipelineId,
                JobInfo.BranchName
            ORDER BY
                MIN(JobInfo.StartDateTime) DESC
            LIMIT 200
        ", ct);
        var result = await reader.ReadAllAsync<PipelineRunQueryResult>(ct);
        return result.ToArray();
    }

    public static async Task<PipelineRunQueryResult[]> GetPipelineRunsOverview(
        this TestCityDatabase db,
        string[] projectIds,
        string? currentBranchName = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        var reader = await connection.ExecuteQueryAsync($@"
            SELECT 
                ProjectId,
                PipelineId,
                BranchName,
                StartDateTime,
                TotalTestsCount,
                Duration,
                SuccessTestsCount,
                SkippedTestsCount,
                FailedTestsCount,
                State,
                JobRunCount,
                CustomStatusMessage,
                CommitMessage,
                CommitAuthor,
                CommitSha,
                HasCodeQualityReport,
                ChangesSinceLastRun,
                TotalCoveredCommitCount
            FROM ( 
                SELECT 
                    ProjectId as ProjectId ,
                    PipelineId as PipelineId,
                    BranchName as BranchName,
                    MIN(StartDateTime) as StartDateTime,
                    SUM(TotalTestsCount) as TotalTestsCount,
                    SUM(Duration) as Duration,
                    SUM(SuccessTestsCount) as SuccessTestsCount,
                    SUM(SkippedTestsCount) as SkippedTestsCount,
                    SUM(FailedTestsCount) as FailedTestsCount,
                    MAX(State) as State,
                    COUNT(JobRunId) as JobRunCount,
                    arrayStringConcat(groupArrayIf(JobInfo.CustomStatusMessage, JobInfo.CustomStatusMessage != ''), ', ') as CustomStatusMessage,
                    ROW_NUMBER() OVER (PARTITION BY JobInfo.ProjectId, JobInfo.BranchName ORDER BY MAX(JobInfo.StartDateTime) DESC) AS rn,
                    arrayElement(topK(1)(CommitMessage), 1) as CommitMessage,
                    arrayElement(topK(1)(CommitAuthor), 1) as CommitAuthor,
                    arrayElement(topK(1)(CommitSha), 1) as CommitSha,
                    MAX(HasCodeQualityReport) as HasCodeQualityReport,
                    arraySlice(any(ji.ChangesSinceLastRun), 1, 20) as ChangesSinceLastRun,
                    length(any(ji.ChangesSinceLastRun)) as TotalCoveredCommitCount
                FROM JobInfo ji
                WHERE 
                    JobInfo.PipelineId <> ''
                    AND JobInfo.ProjectId IN {projectIds}
                    AND ({currentBranchName} IS NULL OR JobInfo.BranchName = {currentBranchName})
                GROUP BY 
                    JobInfo.ProjectId, 
                    JobInfo.PipelineId, 
                    JobInfo.BranchName
                ORDER BY
                    MAX(JobInfo.StartDateTime) DESC
            ) filtered
            WHERE 
            rn = 1
            LIMIT 1000
        ", ct);
        var result = await reader.ReadAllAsync<PipelineRunQueryResult>(ct);
        return result.ToArray();
    }

}
