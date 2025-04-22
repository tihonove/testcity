import { ClickHouseClient } from "@clickhouse/client-web";
import { uuidv4 } from "../../Utils/Guids";
import { reject } from "../../Utils/TypeHelpers";
import { JobIdWithParentProject } from "../JobIdWithParentProject";
import { PipelineRunsQueryRow } from "../PipelineRunsQueryRow";
import { TestPerJobRunQueryRow } from "../TestPerJobRunQueryRow";
import { JobRunFullInfoQueryRow, JobsQueryRow } from "./JobsQuery";
import { GroupNode, Project } from "./Projects/GroupNode";
import { TestRunQueryRow } from "./TestRunQuery";

export function findPathToProjectById(groupNode: GroupNode, projectId: string): [GroupNode[], Project] {
    return findPathToProjectByIdOrNull(groupNode, projectId) ?? reject(`Project with id ${projectId} not found`);
}

export function findPathToProjectByIdOrNull(
    groupNode: GroupNode,
    projectId: string
): [GroupNode[], Project] | undefined {
    if ("projects" in groupNode) {
        const project = groupNode.projects?.find(p => p.id === projectId);
        if (project) {
            return [[groupNode], project];
        }
    }

    if ("groups" in groupNode) {
        for (const group of groupNode.groups ?? []) {
            const result = findPathToProjectByIdOrNull(group, projectId);
            if (result) return [[groupNode, ...result[0]], result[1]];
        }
    }

    return undefined;
}

function getQueryId() {
    return uuidv4();
}

export class TestAnalyticsStorage {
    public constructor(client: ClickHouseClient) {
        this.client = client;
    }

    public async getJobInfo(
        projectId: string,
        jobId: string,
        jobRunId: string
    ): Promise<JobRunFullInfoQueryRow | undefined> {
        const query = `
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
                    ji.ChangesSinceLastRun,
                    length(ji.ChangesSinceLastRun) as TotalCoveredCommitCount 
                FROM JobInfo ji
                WHERE ji.ProjectId = '${projectId}' AND ji.JobId = '${jobId}' AND ji.JobRunId = '${jobRunId}'
                `;
        const result = await this.executeClickHouseQuery<JobRunFullInfoQueryRow[]>(query);
        return result[0];
    }

    public async getFailedTestOutput(
        jobId: string,
        testId: string,
        jobRunIds: string[]
    ): Promise<[string, string, string]> {
        const query = `
            SELECT
                JUnitFailureMessage,
                JUnitFailureOutput,
                JUnitSystemOutput
            FROM TestRuns
            WHERE 
                TestId = '${testId}' 
                AND JobId = '${jobId}'
                AND JobRunId IN [${jobRunIds.map(x => `'${x}'`).join(", ")}]
            LIMIT 1
        `;
        const result = await this.executeClickHouseQuery<[string, string, string][]>(query);
        return result[0];
    }

    public async getTestListForCsv(jobRunIds: string[]): Promise<[string, string, string, string][]> {
        const query = `SELECT 
                rowNumberInAllBlocks() + 1, 
                TestId, 
                State, 
                Duration 
            FROM TestRunsByRun 
            WHERE 
                JobRunId in [${jobRunIds.map(x => "'" + x + "'").join(",")}]`;
        const data = await this.executeClickHouseQuery<[string, string, string, string][]>(query);
        return data;
    }

    public async findAllJobs(projectIds: string[]): Promise<JobIdWithParentProject[]> {
        const query = `
            SELECT DISTINCT 
                JobId, ProjectId 
            FROM JobInfo 
            WHERE 
                StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND 
                ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]
        `;

        return this.executeClickHouseQuery<JobIdWithParentProject[]>(query);
    }

    public async findAllJobsRuns(projectIds: string[], currentBranchName?: string): Promise<JobsQueryRow[]> {
        const query = `
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
                ROW_NUMBER() OVER (PARTITION BY JobId ORDER BY StartDateTime DESC) AS rnj
                FROM (
                    SELECT
                        *,
                        ROW_NUMBER() OVER (PARTITION BY JobId, BranchName ORDER BY StartDateTime DESC) AS rn
                    FROM JobInfo 
                    WHERE 
                        StartDateTime >= now() - INTERVAL 30 DAY 
                        AND ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]
                        ${currentBranchName ? `AND BranchName = '${currentBranchName}'` : ""}
                ) AS filtered_inner 
                WHERE rn = 1
            ) AS filtered
            WHERE (rnj <= 5 OR StartDateTime >= now() - INTERVAL 3 DAY)
            ORDER BY filtered.JobId, filtered.StartDateTime DESC
            LIMIT 1000;
        `;

        return this.executeClickHouseQuery<JobsQueryRow[]>(query);
    }

    public async findAllJobsRunsInProgress(projectIds: string[], currentBranchName?: string): Promise<JobsQueryRow[]> {
        const query = `
            SELECT
                ipji.JobId,
                ipji.JobRunId,
                ipji.BranchName,
                ipji.AgentName,
                ipji.StartDateTime,
                null as TotalTestsCount,
                ipji.AgentOSName,
                null as Duration,
                null as SuccessTestsCount,
                null as SkippedTestsCount,
                null as FailedTestsCount,
                'Running' as State,
                null as CustomStatusMessage,
                ipji.JobUrl,
                ipji.ProjectId,
                0 as HasCodeQualityReport
            FROM InProgressJobInfo ipji
            LEFT JOIN JobInfo AS ji ON ji.JobId = ipji.JobId AND ji.JobRunId = ipji.JobRunId
            WHERE 
                ji.JobRunId = ''
                AND ipji.StartDateTime >= now() - INTERVAL 30 DAY 
                AND ipji.ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]
                ${currentBranchName ? `AND ipji.BranchName = '${currentBranchName}'` : ""}
            ORDER BY ipji.JobId, ipji.StartDateTime DESC
            LIMIT 1000;
        `;

        return this.executeClickHouseQuery<JobsQueryRow[]>(query);
    }

    public async findAllJobsRunsPerJobId(
        projectId: string,
        jobId: string,
        currentBranchName?: string,
        page: number = 0
    ): Promise<JobsQueryRow[]> {
        const itemsPerPage = 100;
        const condition = (a: string) => {
            let result = `${a}.JobId = '${jobId}' AND ${a}.ProjectId = '${projectId}'`;
            if (currentBranchName != undefined) result += ` AND ${a}.BranchName = '${currentBranchName}'`;
            return result;
        };

        const query = `
            SELECT * FROM (

                SELECT 

                    ipji.JobId,
                    ipji.JobRunId,
                    ipji.BranchName,
                    ipji.AgentName,
                    ipji.StartDateTime,
                    null as TotalTestsCount,
                    ipji.AgentOSName,
                    null as Duration,
                    null as SuccessTestsCount,
                    null as SkippedTestsCount,
                    null as FailedTestsCount,
                    'Running' as State,
                    '' as CustomStatusMessage,
                    ipji.JobUrl,
                    ipji.ProjectId,
                    0 as HasCodeQualityReport,
                    arraySlice(ipji.ChangesSinceLastRun, 1, 20),
                    length(ipji.ChangesSinceLastRun) as TotalCoveredCommitCount

                FROM InProgressJobInfo ipji
                LEFT JOIN JobInfo eji ON eji.JobId = ipji.JobId AND eji.JobRunId = ipji.JobRunId
                WHERE eji.JobRunId = '' AND ${condition("ipji")}

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
                    arraySlice(ji.ChangesSinceLastRun, 1, 20),
                    length(ji.ChangesSinceLastRun) as TotalCoveredCommitCount

                FROM JobInfo ji
                WHERE ${condition("ji")}

            )
            ORDER BY StartDateTime DESC
            LIMIT ${(itemsPerPage * page).toString()}, ${itemsPerPage.toString()}
        `;

        return this.executeClickHouseQuery<JobsQueryRow[]>(query);
    }

    public async getPipelineRunsOverview(
        projectIds: string[],
        currentBranchName?: string
    ): Promise<PipelineRunsQueryRow[]> {
        const query = `
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
                HasCodeQualityReport
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
                    MAX(HasCodeQualityReport) as HasCodeQualityReport
                FROM JobInfo
                WHERE 
                    JobInfo.PipelineId <> ''
                    AND JobInfo.ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]
                    ${currentBranchName ? `AND JobInfo.BranchName = '${currentBranchName}'` : ""}
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
        `;
        return this.executeClickHouseQuery<PipelineRunsQueryRow[]>(query);
    }

    public async getPipelineRunsByProject(
        projectId: string,
        currentBranchName?: string
    ): Promise<PipelineRunsQueryRow[]> {
        const query = `
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
                arrayElement(topK(1)(CommitMessage), 1) as CommitMessage,
                arrayElement(topK(1)(CommitAuthor), 1) as CommitAuthor,
                arrayElement(topK(1)(CommitSha), 1) as CommitSha
            FROM JobInfo
            WHERE 
                JobInfo.PipelineId <> ''
                AND JobInfo.ProjectId = '${projectId}'
                ${currentBranchName ? `AND JobInfo.BranchName = '${currentBranchName}'` : ""}
            GROUP BY 
                JobInfo.ProjectId,
                JobInfo.PipelineId,
                JobInfo.BranchName
            ORDER BY
                MIN(JobInfo.StartDateTime) DESC
            LIMIT 200
        `;
        return this.executeClickHouseQuery<PipelineRunsQueryRow[]>(query);
    }

    public async getPipelineInfo(pipelineId: string): Promise<PipelineInfo> {
        const query = `
                SELECT 
                    ProjectId,
                    PipelineId as PipelineId,
                    BranchName as BranchName,
                    MIN(StartDateTime) as StartDateTime,
                    MAX(EndDateTime) as EndDateTime,
                    SUM(TotalTestsCount) as TotalTestsCount,
                    SUM(Duration) as Duration,
                    SUM(SuccessTestsCount) as SuccessTestsCount,
                    SUM(SkippedTestsCount) as SkippedTestsCount,
                    SUM(FailedTestsCount) as FailedTestsCount,
                    MAX(State) as State,
                    COUNT(JobRunId) as JobRunCount,
                    arrayStringConcat(groupArrayIf(JobInfo.CustomStatusMessage, JobInfo.CustomStatusMessage != ''), ', ') as CustomStatusMessage,
                    arrayStringConcat(groupArrayIf(JobInfo.JobRunId, JobInfo.JobRunId != ''), ';') as JobRunIds,
                    arrayElement(topK(1)(CommitSha), 1) as CommitSha,
                    arrayElement(topK(1)(CommitMessage), 1) as CommitMessage,
                    arrayElement(topK(1)(CommitAuthor), 1) as CommitAuthor,
                    arrayElement(topK(1)(Triggered), 1) as Triggered,
                    arrayElement(topK(1)(PipelineSource), 1) as PipelineSource
                FROM JobInfo
                WHERE PipelineId = '${pipelineId}'
                GROUP BY PipelineId, BranchName, ProjectId
        `;
        const data =
            await this.executeClickHouseQuery<
                [
                    string,
                    string,
                    string,
                    string,
                    string,
                    string,
                    number,
                    string,
                    string,
                    string,
                    "Success" | "Failed" | "Canceled" | "Timeouted",
                    string,
                    string,
                    string,
                    string,
                    string,
                    string,
                    string,
                    string,
                ][]
            >(query);
        // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
        if (data[0] == undefined) {
            throw new Error("Pipeline not found");
        }
        return {
            projectId: data[0][0],
            pipelineId: data[0][1],
            branchName: data[0][2],
            startDateTime: data[0][3],
            endDateTime: data[0][4],
            totalTestsCount: Number(data[0][5]),
            duration: data[0][6],
            successTestsCount: Number(data[0][7]),
            skippedTestsCount: Number(data[0][8]),
            failedTestsCount: Number(data[0][9]),
            state: data[0][10],
            jobRunCount: Number(data[0][11]),
            customStatusMessage: data[0][12],
            jobRunIds: data[0][13].split(";"),
            commitSha: data[0][14],
            commitMessage: data[0][15],
            commitAuthor: data[0][16],
            triggered: data[0][17],
            pipelineSource: data[0][18],
        };
    }

    public async getTestList(
        jobRunIds: string[],
        sortField?: "State" | "TestId" | "Duration" | "StartDateTime",
        sortDirection?: "ASC" | "DESC",
        testIdQuery?: string,
        testStateFilter?: "Failed" | "Success" | "Skipped",
        itemsPerPage: number = 100,
        page: number = 0
    ): Promise<TestRunQueryRow[]> {
        let condition = `JobRunId in [${jobRunIds.map(x => "'" + x + "'").join(",")}]`;
        if (testIdQuery?.trim()) condition += ` AND TestId LIKE '%${testIdQuery}%'`;
        let havingCondition = "";
        const finalStateExpression =
            "if(has(groupArray(t.State), 'Success'),  'Success', if(has(groupArray(t.State), 'Failed'), 'Failed', any(t.State)))";
        if (testStateFilter != undefined) havingCondition = `${finalStateExpression} = '${testStateFilter}'`;

        const query = `
            SELECT 
                ${finalStateExpression} AS FinalState,
                TestId,
                avg(Duration) AS AvgDuration,
                min(Duration) AS MinDuration,
                max(Duration) AS MaxDuration,
                any(JobId) AS JobId,
                arrayStringConcat(groupArray(t.State), ',') AS AllStates,
                min(StartDateTime) AS StartDateTime,
                count() AS TotalRuns,
                sum(
                    CASE 
                        WHEN t.State = 'Failed' THEN 100
                        WHEN t.State = 'Success' THEN 1
                        WHEN t.State = 'Skipped' THEN 0
                        ELSE 0
                    END
                ) AS StateWeight
            FROM TestRunsByRun t
            WHERE ${condition} 
            GROUP BY TestId
            ${havingCondition ? `HAVING ${havingCondition}` : ""}
            ORDER BY ${sortField ?? "StateWeight"}  ${sortField ? (sortDirection ?? "ASC") : "DESC"}
            LIMIT ${(itemsPerPage * page).toString()}, ${itemsPerPage.toString()}
            `;
        return await this.executeClickHouseQuery<TestRunQueryRow[]>(query);
    }

    public async findBranches(projectIds?: string[], jobId?: string): Promise<string[]> {
        const query = `
            SELECT DISTINCT 
                BranchName
            FROM JobInfo
            WHERE 
                StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND BranchName != '' 
                ${projectIds ? `AND ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]` : ""}
                ${jobId ? `AND JobId = '${jobId}'` : ""}
            ORDER BY StartDateTime DESC;
        `;
        const result = await this.executeClickHouseQuery<[string][]>(query);
        return result.map(row => row[0]);
    }

    private async executeClickHouseQuery<T>(query: string): Promise<T> {
        const id = getQueryId();
        const response = await this.client.query({ query: query, format: "JSONCompact", query_id: id });
        const result = await response.json();
        if (typeof result === "object") {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            return result["data"];
        } else {
            throw new Error("Invalid output");
        }
    }

    public async getTestStats(
        testId: string,
        jobIds: string[],
        branchName?: string
    ): Promise<[string, number, string][]> {
        let condition = `TestId = '${testId}'`;
        condition += ` AND JobId IN [${jobIds.map(x => `'${x}'`).join(", ")}]`;
        if (branchName != undefined) condition += ` AND BranchName = '${branchName}'`;
        const query = `
            SELECT
                State, 
                Duration, 
                StartDateTime 
            FROM TestRuns 
            WHERE ${condition} 
            ORDER BY StartDateTime DESC
            LIMIT 1000`;
        return this.executeClickHouseQuery<[string, number, string][]>(query);
    }

    public async getTestRuns(
        testId: string,
        jobIds: string[],
        branchName?: string,
        page: number = 0,
        pageSize: number = 50
    ): Promise<TestPerJobRunQueryRow[]> {
        let condition = `TestRuns.TestId = '${testId}'`;
        condition += ` AND TestRuns.JobId IN [${jobIds.map(x => `'${x}'`).join(", ")}]`;
        if (branchName != undefined) condition += ` AND TestRuns.BranchName = '${branchName}'`;

        const query = `
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
        WHERE ${condition} 
        ORDER BY TestRuns.StartDateTime DESC 
        LIMIT ${(pageSize * page).toString()}, ${pageSize.toString()}`;
        return this.executeClickHouseQuery<TestPerJobRunQueryRow[]>(query);
    }

    public async getTestRunCount(testId: string, jobIds: string[], branchName?: string): Promise<number> {
        let condition = `TestId = '${testId}'`;
        condition += ` AND JobId IN [${jobIds.map(x => `'${x}'`).join(", ")}]`;
        if (branchName != undefined) condition += ` AND BranchName = '${branchName}'`;

        const query = `SELECT COUNT(*) FROM TestRuns WHERE ${condition}`;
        const result = await this.executeClickHouseQuery<[string][]>(query);
        return Number(result[0][0]);
    }

    private client: ClickHouseClient;
}

interface PipelineInfo {
    projectId: string;
    pipelineId: string;
    branchName: string;
    startDateTime: string;
    endDateTime: string;
    totalTestsCount: number;
    duration: number;
    successTestsCount: number;
    skippedTestsCount: number;
    failedTestsCount: number;
    state: "Success" | "Failed" | "Canceled" | "Timeouted";
    jobRunCount: number;
    customStatusMessage: string;
    jobRunIds: string[];
    commitSha: string;
    commitMessage: string;
    commitAuthor: string;
    triggered: string;
    pipelineSource: string;
}
