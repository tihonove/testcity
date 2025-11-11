import { RunStatus } from "../Components/RunStatus";
import { JobsQueryRow } from "./Storage/JobsQuery";
import { Group, Project } from "./Storage/Projects/GroupNode";

export interface GroupOrProjectPathSlugItem {
    id: string;
    title: string;
    avatarUrl: string | null;
}

export interface GroupDashboardNode {
    id: string;
    title: string;
    avatarUrl: string | null;
    type: "group";
    link: string;
    fullPathSlug: GroupOrProjectPathSlugItem[];
    children: DashboardNode[];
}

export interface ProjectDashboardNode {
    id: string;
    title: string;
    avatarUrl: string | null;
    type: "project";
    link: string;
    gitLabLink: string;
    fullPathSlug: GroupOrProjectPathSlugItem[];
    jobs: JobDashboardInfo[];
}

export type DashboardNode = GroupDashboardNode | ProjectDashboardNode;

export interface JobDashboardInfo {
    jobId: string;
    runs: JobRun[];
}

export interface CommitParentsChangesEntry {
    parentCommitSha: string;
    depth: number;
    authorName: string;
    authorEmail: string;
    messagePreview: string;
}

export interface JobRun {
    jobId: string;
    jobRunId: string;
    branchName: string;
    agentName: string;
    startDateTime: string;
    totalTestsCount: number | null;
    agentOSName: string;
    duration: number | null;
    successTestsCount: number | null;
    skippedTestsCount: number | null;
    failedTestsCount: number | null;
    state: string;
    customStatusMessage: string;
    jobUrl: string;
    projectId: string;
    hasCodeQualityReport: boolean;
    changesSinceLastRun: CommitParentsChangesEntry[];
    totalCoveredCommitCount: number;
}

export interface JobInfo {
    jobId: string;
    jobRunId: string;
    branchName: string;
    agentName: string;
    startDateTime: string;
    endDateTime: string;
    totalTestsCount: number | null;
    agentOSName: string;
    duration: number | null;
    successTestsCount: number | null;
    skippedTestsCount: number | null;
    failedTestsCount: number | null;
    state: string;
    customStatusMessage: string;
    jobUrl: string;
    projectId: string;
    pipelineSource: string | null;
    triggered: string | null;
    hasCodeQualityReport: boolean;
    changesSinceLastRun: CommitParentsChangesEntry[];
    totalCoveredCommitCount: number;
    pipelineId: string | null;
    commitSha: string | null;
    commitMessage: string | null;
    commitAuthor: string | null;
}

export interface TestRun {
    finalState: RunStatus;
    testId: string;
    avgDuration: number;
    minDuration: number;
    maxDuration: number;
    jobId: string;
    allStates: string;
    startDateTime: string;
    totalRuns: number;
}
