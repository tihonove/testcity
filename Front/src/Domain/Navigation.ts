import { findPathToProjectById } from "./Storage/Storage";
import { GroupNode, Project } from "./Storage/Projects/GroupNode";

// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
// eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
export const urlPrefix: string = window.__webpack_public_path__;

// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
// eslint-disable-next-line @typescript-eslint/restrict-plus-operands
export const apiUrlPrefix: string = (window.__webpack_public_path__ ?? "/") + "api/";

export function createLinkToTestHistory(basePrefix: string, testId: string, pathToProject: string[]): string {
    const result = `${basePrefix}${[...pathToProject, "test-history"].join("/")}?id=${encodeURIComponent(testId)}`;
    return result;
}

export function createLinkToProject(groupNode: GroupNode, projectId: string): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return urlPrefix + [...groups.map(x => x.title), project.title].map(x => encodeURIComponent(x)).join("/");
}

export function createLinkToGroupOrProject(nodesPath: (GroupNode | Project)[]): string {
    return urlPrefix + [...nodesPath.map(x => x.title)].map(x => encodeURIComponent(x)).join("/");
}

export function createLinkToGitLabProject(groupNode: GroupNode, projectId: string, branchName?: string): string {
    const [groups, project] = findPathToProjectById(groupNode, projectId);
    return (
        "https://git.skbkontur.ru/" +
        [...groups.map(x => x.title), project.title].map(x => encodeURIComponent(x)).join("/") +
        (branchName ? `?ref=${encodeURIComponent(branchName)}` : "")
    );
}

export function addBranchToLink(groupOrProjectLink: string, currentBranchName?: string): string {
    return groupOrProjectLink + (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "");
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
        [...groups.map(x => x.title), project.title, "jobs", jobId].map(x => encodeURIComponent(x)).join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToJob2(projectLink: string, jobId: string, currentBranchName?: string): string {
    return (
        projectLink +
        "/" +
        ["jobs", jobId].map(x => encodeURIComponent(x)).join("/") +
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
        [...groups.map(x => x.title), project.title, "jobs", jobId, "runs", jobRunId]
            .map(x => encodeURIComponent(x))
            .join("/") +
        (currentBranchName ? `?branch=${encodeURIComponent(currentBranchName)}` : "")
    );
}

export function createLinkToJobRun2(
    projectLink: string,
    jobId: string,
    jobRunId: string,
    currentBranchName?: string
): string {
    return (
        projectLink +
        "/" +
        ["jobs", jobId, "runs", jobRunId].map(x => encodeURIComponent(x)).join("/") +
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

export function createLinkToCreateNewPipeline2(gitlabProjectLink: string, branchName?: string): string {
    return (
        gitlabProjectLink +
        "/" +
        ["-", "pipelines", "new"].map(x => encodeURIComponent(x)).join("/") +
        (branchName ? `?ref=${encodeURIComponent(branchName)}` : "")
    );
}

export function useBasePrefix(): string {
    return urlPrefix;
}

export function useApiUrl(): string {
    return apiUrlPrefix;
}

export function groupLink(basePrefix: string, groupIdOrTitleList: string[], branchName?: string): string {
    return (
        `${basePrefix}${groupIdOrTitleList.map(x => encodeURIComponent(x)).join("/")}` +
        (branchName ? `?branch=${encodeURIComponent(branchName)}` : "")
    );
}

export function getLinkToPipeline(pathToProject: string[], pipelineId: string): string {
    return (
        "https://git.skbkontur.ru/" +
        [...pathToProject, "-", "pipelines", pipelineId].map(x => encodeURIComponent(x)).join("/")
    );
}

export function getLinkToCommit(pathToProject: string[], sha: string): string {
    return (
        "https://git.skbkontur.ru/" + [...pathToProject, "-", "commit", sha].map(x => encodeURIComponent(x)).join("/")
    );
}
