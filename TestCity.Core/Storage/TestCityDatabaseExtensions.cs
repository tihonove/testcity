using TestCity.Clickhouse;
using TestCity.Core.Storage.DTO;

namespace TestCity.Core.Storage;

public static class TestCityDatabaseExtensions
{
    public static async Task<JobInfoQueryResult?> GetJobInfo(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        string jobRunId,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT 
                ji.JobId,
                ji.JobRunId,
                ji.BranchName,
                ji.AgentName,
                ji.StartDateTime,
                ji.EndDateTime,
                ji.TotalTestsCount,
                ji.AgentOSName,
                ji.Duration,
                ji.SuccessTestsCount,
                ji.SkippedTestsCount,
                ji.FailedTestsCount,
                ji.State,
                ji.CustomStatusMessage,
                ji.JobUrl,
                ji.ProjectId,
                ji.PipelineSource,
                ji.Triggered,
                ji.HasCodeQualityReport,
                arraySlice(ji.ChangesSinceLastRun, 1, 20) as ChangesSinceLastRun,
                length(ji.ChangesSinceLastRun) as TotalCoveredCommitCount,
                ji.PipelineId,
                ji.CommitSha,
                ji.CommitMessage,
                ji.CommitAuthor
            FROM JobInfo ji
            WHERE 
                ji.ProjectId = {projectId} 
                AND ji.JobId = {jobId} 
                AND ji.JobRunId = {jobRunId}
        ", ct);
        return await reader.ReadSingleAsync<JobInfoQueryResult>(ct);
    }

    public static async Task<TestOutput?> GetTestOutput(
        this TestCityDatabase db,
        string jobId,
        string testId,
        string[] jobRunIds,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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

        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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
                arraySlice(ipji.ChangesSinceLastRun, 1, 20) as ChangesSinceLastRunTuple,
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

        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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
                arraySlice(filtered.ChangesSinceLastRun, 1, 20) as ChangesSinceLastRunTuple,
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

    public static async Task<JobRunQueryResult[]> FindAllJobsRunsPerJobId(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        string? currentBranchName = null,
        int page = 0,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        const int itemsPerPage = 100;

        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT * FROM (
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
                    arraySlice(ipji.ChangesSinceLastRun, 1, 20) as ChangesSinceLastRunTuple,
                    length(ipji.ChangesSinceLastRun) as TotalCoveredCommitCount
                FROM InProgressJobInfo ipji
                LEFT JOIN JobInfo eji ON eji.JobId = ipji.JobId AND eji.JobRunId = ipji.JobRunId
                WHERE 
                    eji.JobRunId = ''
                    AND ipji.JobId = {jobId}
                    AND ipji.ProjectId = {projectId}
                    AND ({currentBranchName} IS NULL OR ipji.BranchName = {currentBranchName})

                UNION ALL

                SELECT 
                    ji.JobId,
                    ji.JobRunId,
                    ji.BranchName,
                    ji.AgentName,
                    ji.StartDateTime,
                    ji.TotalTestsCount,
                    ji.AgentOSName,
                    ji.Duration,
                    ji.SuccessTestsCount,
                    ji.SkippedTestsCount,
                    ji.FailedTestsCount,
                    ji.State,
                    ji.CustomStatusMessage,
                    ji.JobUrl,
                    ji.ProjectId,
                    ji.HasCodeQualityReport,
                    arraySlice(ji.ChangesSinceLastRun, 1, 20) as ChangesSinceLastRunTuple,
                    length(ji.ChangesSinceLastRun) as TotalCoveredCommitCount
                FROM JobInfo ji
                WHERE 
                    ji.JobId = {jobId}
                    AND ji.ProjectId = {projectId}
                    AND ({currentBranchName} IS NULL OR ji.BranchName = {currentBranchName})
            )
            ORDER BY StartDateTime DESC
            LIMIT {itemsPerPage * page}, {itemsPerPage}
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

        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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

        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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
                arraySlice(any(ji.ChangesSinceLastRun), 1, 20) as ChangesSinceLastRunTuple,
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

        var reader = await connection.ExecuteQueryAsyncWithParams($@"
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
                ChangesSinceLastRun as ChangesSinceLastRunTuple,
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

    public static async Task<int> GetFlakyTestsCount(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        double flipRateThreshold = 0.1,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT COUNT(*) as TotalCount
            FROM (
                SELECT 
                    ProjectId,
                    JobId,
                    TestId,
                    argMax(t.FlipCount, t.UpdatedAt) / argMax(t.RunCount, t.UpdatedAt) as FlipRate
                FROM TestDashboardWeekly t
                WHERE
                    ProjectId = {projectId}
                    AND JobId = {jobId}
                    AND t.LastRunDate >= now() - INTERVAL 7 DAY
                GROUP BY ProjectId, JobId, TestId
                HAVING 
                    argMax(t.RunCount, t.UpdatedAt) > 20 AND
                    argMax(t.FlipCount, t.UpdatedAt) / argMax(t.RunCount, t.UpdatedAt) > {flipRateThreshold}
            ) as subquery
        ", ct);
        
        if (!await reader.ReadAsync(ct))
            return 0;
            
        return Convert.ToInt32(reader.GetValue(0));
    }

    public static async Task<TestRunQueryResult[]> GetTestList(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        string[] jobRunIds,
        string? sortField = null,
        string? sortDirection = null,
        string? testIdQuery = null,
        string? testStateFilter = null,
        int itemsPerPage = 100,
        int page = 0,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        // NOTE 
        // We need projectId here but currect db schema does not have ProjectId in TestRunsByRun table. We need to update schema later.

        // Build WHERE condition
        var jobRunIdsEscaped = string.Join(",", jobRunIds.Select(id => $"'{EscapeSql(id)}'"));
        var condition = $"t.JobId = '{EscapeSql(jobId)}' AND t.JobRunId in [{jobRunIdsEscaped}]";
        
        if (!string.IsNullOrWhiteSpace(testIdQuery))
        {
            condition += $" AND TestId ILIKE '%{EscapeSql(testIdQuery)}%'";
        }

        // Build HAVING condition for state filter
        const string finalStateExpression = 
            "if(has(groupArray(t.State), 'Success'),  'Success', if(has(groupArray(t.State), 'Failed'), 'Failed', any(t.State)))";
        
        var havingCondition = "";
        if (!string.IsNullOrWhiteSpace(testStateFilter))
        {
            havingCondition = $"HAVING {finalStateExpression} = '{EscapeSql(testStateFilter)}'";
        }

        // Determine sorting - use aliases or aggregated fields for ORDER BY
        var orderByClause = sortField switch
        {
            "State" => $"ORDER BY FinalState {(sortDirection ?? "ASC")}",
            "TestId" => $"ORDER BY TestId {(sortDirection ?? "ASC")}",
            "Duration" => $"ORDER BY AvgDuration {(sortDirection ?? "ASC")}",
            "StartDateTime" => $"ORDER BY StartDateTime {(sortDirection ?? "ASC")}",
            _ => "ORDER BY StateWeight DESC"
        };

        var query = $@"
            SELECT 
                {finalStateExpression} AS FinalState,
                TestId,
                avg(Duration) AS AvgDuration,
                min(Duration) AS MinDuration,
                max(Duration) AS MaxDuration,
                any(JobId) AS JobId,
                arrayStringConcat(groupArray(t.State), ',') AS AllStates,
                min(StartDateTime) AS StartDateTime,
                count() AS TotalRuns,
                sum(multiIf(t.State = 'Failed', 100, t.State = 'Success', 1, t.State = 'Skipped', 0, 0)) AS StateWeight
            FROM TestRunsByRun t
            WHERE {condition} 
            GROUP BY TestId
            {havingCondition}
            {orderByClause}
            LIMIT {itemsPerPage * page}, {itemsPerPage}
        ";

        var reader = await connection.ExecuteQueryAsync(query, ct);
        var result = await reader.ReadAllAsync<TestRunQueryResult>(ct);
        return result.ToArray();
    }

    private static string EscapeSql(string value)
    {
        return value.Replace("'", "''").Replace("\\", "\\\\");
    }

    public static async Task<TestListStats?> GetTestListStats(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        string[] jobRunIds,
        string? testIdQuery = null,
        string? testStateFilter = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();

        // Build WHERE condition
        var jobRunIdsEscaped = string.Join(",", jobRunIds.Select(id => $"'{EscapeSql(id)}'"));
        var condition = $"t.JobId = '{EscapeSql(jobId)}' AND t.JobRunId in [{jobRunIdsEscaped}]";
        
        if (!string.IsNullOrWhiteSpace(testIdQuery))
        {
            condition += $" AND TestId ILIKE '%{EscapeSql(testIdQuery)}%'";
        }

        // Build HAVING condition for state filter
        const string finalStateExpression = 
            "if(has(groupArray(t.State), 'Success'),  'Success', if(has(groupArray(t.State), 'Failed'), 'Failed', any(t.State)))";
        
        var havingCondition = "";
        if (!string.IsNullOrWhiteSpace(testStateFilter))
        {
            havingCondition = $"HAVING {finalStateExpression} = '{EscapeSql(testStateFilter)}'";
        }

        var query = $@"
            SELECT 
                COUNT(TestId) AS totalTestsCount,
                SUM(CASE WHEN FinalState = 'Success' THEN 1 ELSE 0 END) AS successTestsCount,
                SUM(CASE WHEN FinalState = 'Skipped' THEN 1 ELSE 0 END) AS skippedTestsCount,
                SUM(CASE WHEN FinalState = 'Failed' THEN 1 ELSE 0 END) AS failedTestsCount
            FROM (
                SELECT 
                    TestId,
                    {finalStateExpression} AS FinalState
                FROM TestRunsByRun t
                WHERE {condition}
                GROUP BY TestId
                {havingCondition}
            ) grouped_tests
        ";

        var reader = await connection.ExecuteQueryAsync(query, ct);
        return await reader.ReadSingleAsync<TestListStats>(ct);
    }

    public static async Task<TestStatsQueryResult[]> GetTestStats(
        this TestCityDatabase db,
        string testId,
        string[] jobIds,
        string? branchName = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT
                State, 
                Duration, 
                StartDateTime 
            FROM TestRuns 
            WHERE 
                TestId = {testId}
                AND JobId IN {jobIds}
                AND ({branchName} IS NULL OR BranchName = {branchName})
            ORDER BY StartDateTime DESC
            LIMIT 1000
        ", ct);
        var result = await reader.ReadAllAsync<TestStatsQueryResult>(ct);
        return result.ToArray();
    }

    public static async Task<FlakyTestQueryResult[]> GetFlakyTests(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        int limit = 100,
        int offset = 0,
        double flipRateThreshold = 0.1,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT 
                ProjectId,
                JobId,
                TestId,
                argMax(LastRunDate, t.UpdatedAt) as LastRunDate,
                argMax(RunCount, t.UpdatedAt) as RunCount,
                argMax(FailCount, t.UpdatedAt) as FailCount,
                argMax(FlipCount, t.UpdatedAt) as FlipCount,
                max(t.UpdatedAt) as UpdatedAt,
                argMax(t.FlipCount, t.UpdatedAt) / argMax(t.RunCount, t.UpdatedAt) as FlipRate
            FROM TestDashboardWeekly t
            WHERE
                ProjectId = {projectId}
                AND JobId = {jobId}
                AND t.LastRunDate >= now() - INTERVAL 7 DAY
            GROUP BY ProjectId, JobId, TestId
            HAVING 
                argMax(t.RunCount, t.UpdatedAt) > 20 AND
                argMax(t.FlipCount, t.UpdatedAt) / argMax(t.RunCount, t.UpdatedAt) > {flipRateThreshold}
            ORDER BY FlipRate DESC
            LIMIT {limit}
            OFFSET {offset}
        ", ct);
        var result = await reader.ReadAllAsync<FlakyTestQueryResult>(ct);
        return result.ToArray();
    }

    public static async Task<string[]> GetFlakyTestNames(
        this TestCityDatabase db,
        string projectId,
        string jobId,
        double flipRateThreshold = 0.1,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT 
                TestId
            FROM TestDashboardWeekly t
            WHERE
                ProjectId = {projectId}
                AND JobId = {jobId}
                AND t.LastRunDate >= now() - INTERVAL 7 DAY
            GROUP BY ProjectId, JobId, TestId
            HAVING 
                argMax(t.RunCount, t.UpdatedAt) > 20 AND
                argMax(t.FlipCount, t.UpdatedAt) / argMax(t.RunCount, t.UpdatedAt) > {flipRateThreshold}
            LIMIT 1000
        ", ct);

        var testNames = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            testNames.Add(reader.GetString(0));
        }
        return testNames.ToArray();
    }

    public static async Task<TestPerJobRunQueryResult[]> GetTestRuns(
        this TestCityDatabase db,
        string testId,
        string[] jobIds,
        string? branchName = null,
        int page = 0,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT 
                TestRuns.JobId, 
                TestRuns.JobRunId, 
                TestRuns.BranchName, 
                TestRuns.State, 
                TestRuns.Duration, 
                TestRuns.StartDateTime, 
                TestRuns.JobUrl,
                JobInfo.CustomStatusMessage
            FROM TestRuns 
            ANY INNER JOIN JobInfo ON JobInfo.JobRunId = TestRuns.JobRunId 
            WHERE 
                TestRuns.TestId = {testId}
                AND TestRuns.JobId IN {jobIds}
                AND ({branchName} IS NULL OR TestRuns.BranchName = {branchName})
            ORDER BY TestRuns.StartDateTime DESC 
            LIMIT {pageSize * page}, {pageSize}
        ", ct);
        var result = await reader.ReadAllAsync<TestPerJobRunQueryResult>(ct);
        return result.ToArray();
    }

    public static async Task<int> GetTestRunCount(
        this TestCityDatabase db,
        string testId,
        string[] jobIds,
        string? branchName = null,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        var reader = await connection.ExecuteQueryAsyncWithParams($@"
            SELECT COUNT(*) 
            FROM TestRuns 
            WHERE 
                TestId = {testId}
                AND JobId IN {jobIds}
                AND ({branchName} IS NULL OR BranchName = {branchName})
        ", ct);
        
        if (!await reader.ReadAsync(ct))
            return 0;
            
        return Convert.ToInt32(reader.GetValue(0));
    }

    public static async Task<TestRunForCsvQueryResult[]> GetTestListForCsv(
        this TestCityDatabase db,
        string jobId,
        string[] jobRunIds,
        CancellationToken ct = default)
    {
        await using var connection = db.ConnectionFactory.CreateConnection();
        
        var jobRunIdsEscaped = string.Join(",", jobRunIds.Select(id => $"'{EscapeSql(id)}'"));
        
        var query = $@"
            SELECT 
                rowNumberInAllBlocks() + 1 as RowNumber, 
                TestId, 
                State, 
                Duration 
            FROM TestRunsByRun 
            WHERE 
                JobId = '{EscapeSql(jobId)}' AND
                JobRunId IN [{jobRunIdsEscaped}]
        ";

        var reader = await connection.ExecuteQueryAsync(query, ct);
        var result = await reader.ReadAllAsync<TestRunForCsvQueryResult>(ct);
        return result.ToArray();
    }

}
