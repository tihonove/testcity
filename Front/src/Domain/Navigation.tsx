import { GroupNode, Project, findPathToProjectById } from "./Storage";
import { basePrefix } from "./BasePrefix";

export const urlPrefix = "/" + basePrefix;

export function createLinkToTestHistory(
    basePrefix: string,
    testId: string,
    pathToProject: string[],
    jobRunId?: string
): string {
    let result = `/${basePrefix}/history?id=${encodeURIComponent(testId)}`;
    if (jobRunId) result += `&runId=${jobRunId}`;
    return result;
}

export function createLinkToProject(groupNode: GroupNode, projectId: string, currentBranchName?: string): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        urlPrefix +
        "/" +
        [...groups.map(x => x.title), project.title].map(x => encodeURIComponent(x)).join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToGroupOrProject(nodesPath: (GroupNode | Project)[], currentBranchName?: string): string {
    return (
        urlPrefix +
        "/" +
        [...nodesPath.map(x => x.title)].map(x => encodeURIComponent(x)).join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToJob(
    groupNode: GroupNode,
    projectId: string,
    jobId: string,
    currentBranchName?: string
): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        urlPrefix +
        "/" +
        [...groups.map(x => x.title), project.title, "jobs", jobId].map(x => encodeURIComponent(x)).join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToJobRun(
    groupNode: GroupNode,
    projectId: string,
    jobId: string,
    jobRunId: string,
    currentBranchName?: string
): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        urlPrefix +
        "/" +
        [...groups.map(x => x.title), project.title, "jobs", jobId, "runs", jobRunId]
            .map(x => encodeURIComponent(x))
            .join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToPipelineRun(
    groupNode: GroupNode,
    projectId: string,
    pipelineId: string,
    currentBranchName?: string
): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        urlPrefix +
        "/" +
        [...groups.map(x => x.title), project.title, "pipelines", pipelineId]
            .map(x => encodeURIComponent(x))
            .join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToCreateNewPipeline(groupNode: GroupNode, projectId: string, branchName?: string): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        "https://git.skbkontur.ru/" +
        [...groups.map(x => x.title), project.title, "-", "pipelines", "new"]
            .map(x => encodeURIComponent(x))
            .join("/") +
        (branchName ? `?ref=${encodeURIComponent(branchName)}` : "")
    );
}

export function useBasePrefix(): string {
    return basePrefix;
}
export function groupLink(basePrefix: string, groupIdOrTitleList: string[], branchName?: string): string {
    return (
        `/${basePrefix}/${groupIdOrTitleList.map(x => encodeURIComponent(x)).join("/")}` +
        (branchName ? `?ref=${encodeURIComponent(branchName)}` : "")
    );
}

export function getLinkToPipeline(pathToProject: string[], pipelineId: string): string {
    return (
        "https://git.skbkontur.ru/" +
        [...pathToProject, "-", "pipelines", pipelineId].map(x => encodeURIComponent(x)).join("/")
    );
}
