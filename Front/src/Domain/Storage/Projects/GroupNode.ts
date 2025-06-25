export interface Group {
    id: string;
    title: string;
    avatarUrl: string;
    mergeRunsFromJobs?: boolean;
}

export interface GroupNode extends Group {
    groups?: GroupNode[];
    projects?: Project[];
}

export interface Project {
    id: string;
    title: string;
    avatarUrl: string;
}

export function isGroup(node: GroupNode | Project): node is GroupNode {
    return "projects" in node || "groups" in node;
}

export function isProject(node: GroupNode | Project): node is Project {
    return !isGroup(node);
}

export function getProjects(node: GroupNode | Project): Project[] {
    if (isGroup(node)) {
        const projects: Project[] = [];
        for (const project of node.projects ?? []) {
            projects.push(project);
        }
        for (const group of node.groups ?? []) {
            projects.push(...getProjects(group));
        }
        return projects;
    }
    return [node];
}

export function resolvePathToNodes(
    topLevelGroup: GroupNode,
    groupIdOrTitleList: string[]
): (GroupNode | Project)[] | undefined {
    const result: (GroupNode | Project)[] = [topLevelGroup];
    for (const groupIdOrTitle of groupIdOrTitleList) {
        const prevNode = result[result.length - 1];
        const nextGroupOrProject =
            ("groups" in prevNode
                ? prevNode.groups?.find(
                      x =>
                          x.id === groupIdOrTitle ||
                          x.title === groupIdOrTitle.toLowerCase() ||
                          x.title === groupIdOrTitle
                  )
                : undefined) ??
            ("projects" in prevNode
                ? prevNode.projects?.find(
                      x =>
                          x.id === groupIdOrTitle ||
                          x.title === groupIdOrTitle.toLowerCase() ||
                          x.title === groupIdOrTitle
                  )
                : undefined);
        if (nextGroupOrProject != undefined) {
            result.push(nextGroupOrProject);
        } else {
            return undefined;
        }
    }
    return result;
}
