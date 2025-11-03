import { JobsQueryRow } from "./Storage/JobsQuery";
import { Group, Project } from "./Storage/Projects/GroupNode";

export interface GroupDashboardNode {
    id: string;
    title: string;
    avatarUrl: string;
    type: "group";
    link: string;
    fullPathSlug: (Group | Project)[];
    children: DashboardNode[];
}

export interface ProjectDashboardNode {
    id: string;
    title: string;
    avatarUrl: string;
    type: "project";
    link: string;
    gitLabLink: string;
    fullPathSlug: (Group | Project)[];
    jobs: JobDashboardInfo[];
}

export type DashboardNode = GroupDashboardNode | ProjectDashboardNode;

export interface JobDashboardInfo {
    jobId: string;
    runs: JobsQueryRow[];
}
