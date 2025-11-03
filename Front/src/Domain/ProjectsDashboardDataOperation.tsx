import * as React from "react";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { createLinkToGitLabProject, createLinkToGroupOrProject, createLinkToProject } from "./Navigation";
import { GroupNode, Project, getProjects, isGroup, isProject } from "./Storage/Projects/GroupNode";

import { JobIdWithParentProjectNames } from "./JobIdWithParentProject";
import { DashboardNode, JobDashboardInfo } from "./ProjectDashboardNode";
import { JobRunNames } from "./Storage/JobsQuery";

export function getProjectsDashboardData(
    rootGroup: GroupNode,
    groupNodes: (GroupNode | Project)[],
    currentBranchName?: string
): DashboardNode {
    const currentGroupOrProject = groupNodes[groupNodes.length - 1];
    const projects = getProjects(currentGroupOrProject);
    const projectIds = React.useMemo(() => projects.map(p => p.id), [projects]);
    const pathToGroup = groupNodes.map(x => x.title);

    const allJobs = useStorageQuery(x => x.findAllJobs(projectIds), [projectIds]);
    const inProgressJobRuns = useStorageQuery(
        x => x.findAllJobsRunsInProgress(projectIds, currentBranchName),
        [projectIds, currentBranchName, false, pathToGroup]
    );
    const allJobRuns2 = useStorageQuery(
        x => x.findAllJobsRuns(projectIds, currentBranchName),
        [projectIds, currentBranchName, false, pathToGroup]
    );

    const buildProjectDashboardData = (groupNodes: (GroupNode | Project)[]): DashboardNode => {
        const project = groupNodes[groupNodes.length - 1];
        const allJobs2 = allJobs.filter(j => j[JobIdWithParentProjectNames.ProjectId] === project.id);
        const jobsWithTheirRuns: JobDashboardInfo[] = allJobs2.map(job => {
            const jobId = job[JobIdWithParentProjectNames.JobId];
            const projectId = job[JobIdWithParentProjectNames.ProjectId];
            const jobRuns = [...inProgressJobRuns, ...allJobRuns2].filter(
                x => x[JobRunNames.JobId] === jobId && x[JobRunNames.ProjectId] === projectId
            );
            return { jobId: job[JobIdWithParentProjectNames.JobId], runs: jobRuns };
        });

        return {
            type: "project",
            id: project.id,
            title: project.title,
            avatarUrl: project.avatarUrl,
            fullPathSlug: groupNodes,
            jobs: jobsWithTheirRuns,
            link: createLinkToProject(rootGroup, project.id),
            gitLabLink: createLinkToGitLabProject(rootGroup, project.id),
        };
    };

    const buildGroupDashboardData = (groupNodes: (GroupNode | Project)[]): DashboardNode => {
        const currentGroupOrProject = groupNodes[groupNodes.length - 1];
        if (!isGroup(currentGroupOrProject)) {
            throw new Error("Expected group node");
        }
        return {
            type: "group",
            id: currentGroupOrProject.id,
            title: currentGroupOrProject.title,
            avatarUrl: currentGroupOrProject.avatarUrl,
            fullPathSlug: groupNodes,
            link: createLinkToGroupOrProject(groupNodes),
            children: [
                ...(currentGroupOrProject.projects ?? []).map(p => buildProjectDashboardData([...groupNodes, p])),
                ...(currentGroupOrProject.groups ?? []).map(g => buildGroupDashboardData([...groupNodes, g])),
            ],
        };
    };

    const buildDashboardData = (groupNodes: (GroupNode | Project)[]): DashboardNode => {
        const currentGroupOrProject = groupNodes[groupNodes.length - 1];
        if (isGroup(currentGroupOrProject)) {
            return buildGroupDashboardData([...groupNodes]);
        } else if (isProject(currentGroupOrProject)) {
            return buildProjectDashboardData([...groupNodes]);
        } else {
            throw new Error("Unknown node type");
        }
    };

    return buildDashboardData(groupNodes);
}
