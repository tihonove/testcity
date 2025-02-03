import { GroupNode, findPathToProjectById } from "../Domain/Storage";

export const urlPrefix = "/test-analytics";


export function createLinkToProject(groupNode: GroupNode, projectId: string, currentBranchName?: string): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        urlPrefix +
        "/" +
        [...groups.map(x => x.title), project.title].map(x => encodeURIComponent(x)).join("/") +
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
