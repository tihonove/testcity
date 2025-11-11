import { reject } from "../../Utils/TypeHelpers";
import { GroupNode, Project } from "./Projects/GroupNode";

export function findPathToProjectById(groupNode: GroupNode, projectId: string): [GroupNode[], Project] {
    return findPathToProjectByIdOrNull(groupNode, projectId) ?? reject(`Project with id ${projectId} not found`);
}

export const flipRateThreshold = 0.1;

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
