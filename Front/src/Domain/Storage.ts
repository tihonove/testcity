import { ClickHouseClient } from "@clickhouse/client-web";
import { JobIdWithParentProject } from "./JobIdWithParentProject";
import { uuidv4 } from "./Guids";
import { JobsQueryRow } from "../Components/JobsQueryRow";
import { delay } from "@skbkontur/react-ui/cjs/lib/utils";
import { reject } from "../TypeHelpers";

export interface Group {
    id: string;
    title: string;
}

export interface GroupNode extends Group {
    groups?: GroupNode[];
    projects?: Project[];
}

export interface Project {
    id: string;
    title: string;
}

export function useRootGroups(): Group[] {
    return hardCodedGroups.map(x => ({ id: x.id, title: x.title }));
}

export function findPathToProjectById(groupNode: GroupNode, projectId: string): [GroupNode[], Project] {
    if (groupNode.projects) {
        const project = groupNode.projects.find(p => p.id === projectId);
        if (project) {
            return [[groupNode], project];
        }
    }

    if (groupNode.groups) {
        for (const group of groupNode.groups) {
            const result = findPathToProjectById(group, projectId);
            return [[groupNode, ...result[0]], result[1]];
        }
    }

    throw new Error(`Project with id ${projectId} not found`);
}

const hardCodedGroups: GroupNode[] = [
    {
        id: "7523",
        title: "forms",
        projects: [
            {
                id: "17358",
                title: "forms",
            },
            {
                id: "19371",
                title: "extern.forms",
            },
        ],
    },
    {
        id: "53",
        title: "diadoc",
        projects: [
            {
                id: "182",
                title: "diadoc",
            },
        ],
    },
];

function getQueryId() {
    return uuidv4();
}

export class TestAnalyticsStorage {
    public constructor(client: ClickHouseClient) {
        this.client = client;
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
                ProjectId
            FROM (
                SELECT
                    *,
                    ROW_NUMBER() OVER (PARTITION BY JobId, BranchName ORDER BY StartDateTime DESC) AS rn
                FROM JobInfo 
                WHERE 
                    StartDateTime >= now() - INTERVAL 3 DAY 
                    AND ProjectId IN [${projectIds.map(x => "'" + x + "'").join(", ")}]
                    ${currentBranchName ? `AND BranchName = '${currentBranchName}'` : ""}
            ) AS filtered
            WHERE rn = 1
            ORDER BY JobId, StartDateTime DESC;
        `;

        return this.executeClickHouseQuery<JobsQueryRow[]>(query);
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
                currentGroup = currentGroup.groups?.find(
                    g => g.id === idOrTitle || g.title.toLowerCase() === idOrTitle.toLowerCase()
                );
            }
        }
        return currentGroup?.projects?.find(
            p => p.id === projectIdOrTitleList || p.title.toLowerCase() === projectIdOrTitleList.toLowerCase()
        );
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

    private client: ClickHouseClient;
}
