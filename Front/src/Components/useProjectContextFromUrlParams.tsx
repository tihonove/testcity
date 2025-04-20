import * as React from "react";
import usePromise from "react-promise-suspense";
import { useParams } from "react-router-dom";
import { useApiUrl } from "../Domain/Navigation";
import { Group, GroupNode, Project, resolvePathToNodes } from "../Domain/Storage/Projects/GroupNode";
import { reject } from "../Utils/TypeHelpers";

export function useProjectContextFromUrlParams(): {
    rootGroup: GroupNode;
    groupNodes: (GroupNode | Project)[];
    pathToGroup: string[];
} {
    const { groupIdLevel1, groupIdLevel2, groupIdLevel3 } = useParams();
    if (groupIdLevel1 == null || groupIdLevel1 === "") {
        throw new Error(`Group is not defined`);
    }

    const pathToGroup = React.useMemo(
        () => [groupIdLevel1, groupIdLevel2, groupIdLevel3].filter(x => x != null),
        [groupIdLevel1, groupIdLevel2, groupIdLevel3]
    );
    const rootProjectStructure = useRootGroup(groupIdLevel1);
    const groupNodes = React.useMemo(
        () => resolvePathToNodes(rootProjectStructure, pathToGroup.slice(1)) ?? reject("Undefined is no allowed"),
        [rootProjectStructure, pathToGroup]
    );

    return {
        rootGroup: rootProjectStructure,
        groupNodes: groupNodes,
        pathToGroup: pathToGroup,
    };
}

export function useRootGroup(groupIdOrTitle: string): GroupNode {
    const apiUrl = useApiUrl();
    return usePromise(
        async (x: string) => {
            const response = await fetch(`${apiUrl}/groups/${encodeURIComponent(x)}`);
            if (!response.ok) {
                throw new Error(`Unable to find group ${x}`);
            }
            return (await response.json()) as GroupNode;
        },
        [groupIdOrTitle, "groups"]
    );
}

export function useRootGroups(): Group[] {
    const apiUrl = useApiUrl();
    return usePromise(
        async _ => {
            const response = await fetch(`${apiUrl}groups`);
            if (!response.ok) {
                throw new Error("Unable to load root groups");
            }
            return (await response.json()) as Group[];
        },
        ["groups-root"]
    );
}
