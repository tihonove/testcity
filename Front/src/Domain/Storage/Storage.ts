import { ClickHouseClient } from "@clickhouse/client-web";
import { JobIdWithParentProject } from "../JobIdWithParentProject";
import { uuidv4 } from "../../Utils/Guids";
import { JobsQueryRow } from "./JobsQuery";
import { delay } from "@skbkontur/react-ui/cjs/lib/utils";
import { reject } from "../../Utils/TypeHelpers";
import { PipelineRunsQueryRow } from "../PipelineRunsQueryRow";
import { TestRunQueryRow } from "./TestRunQuery";
import { TestPerJobRunQueryRow } from "../TestPerJobRunQueryRow";

// eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-require-imports
const hardCodedGroups: GroupNode[] = require("../../../gitlab-projects.json");

export interface Group {
    id: string;
    title: string;
    mergeRunsFromJobs?: boolean;
}

export interface GroupNode extends Group {
    groups?: GroupNode[];
    projects?: Project[];
}

export interface Project {
    id: string;
    title: string;
}

export function isGroup(node: GroupNode | Project): node is GroupNode {
    return "projects" in node || "groups" in node;
}

export function isProject(node: GroupNode | Project): node is Project {
    return !isGroup(node);
}

export function useRootGroups(): Group[] {
    return hardCodedGroups.map(x => ({ id: x.id, title: x.title }));
}

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

    public findPathToProjectByIdOrNull(projectId: string): Promise<[GroupNode[], Project] | undefined> {
        for (const groupNode of hardCodedGroups) {
            const projectPath = findPathToProjectByIdOrNull(groupNode, projectId);
            if (projectPath != undefined) {
                return Promise.resolve(projectPath);
            }
        }

        return Promise.resolve(undefined);
    }

    public async findProjectByTestId(testId: string, jobId?: string): Promise<string | undefined> {
        const query = `
            SELECT
                JobInfo.ProjectId 
            FROM TestRuns
            ANY INNER JOIN JobInfo ON TestRuns.JobRunId = JobInfo.JobRunId
            WHERE TestRuns.TestId = '${testId}' ${jobId ? `AND JobInfo.JobId = '${jobId}'` : ""}
            ORDER BY TestRuns.StartDateTime DESC
            LIMIT 1            
        `;
        const result = await this.executeClickHouseQuery<string[]>(query);
        return result[0][0];
    }

    public getPathToProjectById(id: string): (GroupNode | Project)[] | undefined {
        const nodesPath: (GroupNode | Project)[] = [];
        const traverse = (groupNode: GroupNode): boolean => {
            nodesPath.push(groupNode);
            const project = (groupNode.projects ?? []).find(x => x.id === id);
            if (project != undefined) {
                nodesPath.push(project);
                return true;
            }
            for (const childGroup of groupNode.groups ?? []) {
                if (traverse(childGroup)) {
                    return true;
                }
            }
            nodesPath.pop();
            return false;
        };

        for (const rootGroup of hardCodedGroups) {
            if (traverse(rootGroup)) {
                return nodesPath;
            }
        }
        return undefined;
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
                JobId,
                JobRunId,
                BranchName,
                AgentName,
                StartDateTime,
                TotalTestsCount,
                AgentOSName,
                Duration,
                SuccessTestsCount,
                SkippedTestsCount,
                FailedTestsCount,
                State,
                CustomStatusMessage,
                JobUrl,
                ProjectId,
                HasCodeQualityReport
            FROM (
                SELECT
                    *,
                    ROW_NUMBER() OVER (PARTITION BY JobId, BranchName ORDER BY StartDateTime DESC) AS rn
                FROM JobInfo 
                WHERE 
                    StartDateTime >= now() - INTERVAL 30 DAY 
                    AND ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]
                    ${currentBranchName ? `AND BranchName = '${currentBranchName}'` : ""}
            ) AS filtered
            WHERE rn = 1
            ORDER BY JobId, StartDateTime DESC
            LIMIT 1000;
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

    public async executeClickHouseQuery<T>(query: string): Promise<T> {
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

    public async getRootProjectStructure(groupIdOrTitle: string): Promise<GroupNode> {
        await delay(100);
        return (
            hardCodedGroups.find(
                x => x.id === groupIdOrTitle || x.title.toLowerCase() === groupIdOrTitle.toLowerCase()
            ) ?? reject(`Unable to find group ${groupIdOrTitle}`)
        );
    }

    public getProject(idOrTitleList: string[]): Project | undefined {
        const groupIdOrTitleList = idOrTitleList.slice(0, -1);
        const projectIdOrTitleList = idOrTitleList[idOrTitleList.length - 1];
        let currentGroup: GroupNode | undefined;
        for (const idOrTitle of groupIdOrTitleList) {
            if (!currentGroup) {
                currentGroup = hardCodedGroups.find(
                    g => g.id === idOrTitle || g.title.toLowerCase() === idOrTitle.toLowerCase()
                );
            } else {
                currentGroup = (currentGroup.groups ?? []).find(
                    g => g.id === idOrTitle || g.title.toLowerCase() === idOrTitle.toLowerCase()
                );
            }
        }
        return (currentGroup?.projects ?? []).find(
            p => p.id === projectIdOrTitleList || p.title.toLowerCase() === projectIdOrTitleList.toLowerCase()
        );
    }

    public resolvePathToNodes(groupIdOrTitleList: string[]): (GroupNode | Project)[] | undefined {
        const result: (GroupNode | Project)[] = [];
        for (const groupIdOrTitle of groupIdOrTitleList) {
            const prevNode = result[result.length - 1];
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            if (prevNode != undefined) {
                const nextGroupOrProject =
                    ("groups" in prevNode
                        ? prevNode.groups?.find(
                              x => x.id === groupIdOrTitle || x.title === groupIdOrTitle.toLowerCase()
                          )
                        : undefined) ??
                    ("projects" in prevNode
                        ? prevNode.projects?.find(
                              x => x.id === groupIdOrTitle || x.title === groupIdOrTitle.toLowerCase()
                          )
                        : undefined);
                if (nextGroupOrProject != undefined) {
                    result.push(nextGroupOrProject);
                } else {
                    return undefined;
                }
            } else {
                const nextGroup = hardCodedGroups.find(
                    x => x.id === groupIdOrTitle || x.title === groupIdOrTitle.toLowerCase()
                );
                if (nextGroup != undefined) {
                    result.push(nextGroup);
                } else {
                    return undefined;
                }
            }
        }
        return result;
    }

    public getProjects(groupIdOrTitle: string[]): Project[] {
        const findGroup = (groups: GroupNode[], path: string[]): GroupNode | undefined => {
            if (path.length === 0) return undefined;
            const [current, ...rest] = path;
            const group = groups.find(g => g.id === current || g.title.toLowerCase() === current.toLowerCase());
            if (!group) return undefined;
            if (rest.length === 0) return group;

            if (group.groups) return findGroup(group.groups, rest);
            return undefined;
        };

        const findProject = (projects: Project[], idOrTitle: string): Project | undefined => {
            return projects.find(p => p.id === idOrTitle || p.title.toLowerCase() === idOrTitle.toLowerCase());
        };

        const collectProjects = (group: GroupNode): Project[] => {
            let projects = group.projects || [];

            if (group.groups) {
                for (const subGroup of group.groups) {
                    projects = projects.concat(collectProjects(subGroup));
                }
            }
            return projects;
        };

        const groupPath = groupIdOrTitle.slice(0, -1);

        let group = findGroup(hardCodedGroups, groupIdOrTitle);
        if (group) {
            return collectProjects(group);
        }

        const lastElement = groupIdOrTitle[groupIdOrTitle.length - 1];
        group = findGroup(hardCodedGroups, groupPath);
        if (!group) return [];

        const project = findProject(group.projects || [], lastElement);
        if (project) return [project];

        return collectProjects(group);
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
