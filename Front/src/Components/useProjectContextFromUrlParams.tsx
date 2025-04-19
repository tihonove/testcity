import * as React from "react";
import { useParams } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { Group, GroupNode, Project, resolvePathToNodes } from "../Domain/Storage/Projects/GroupNode";
import { reject } from "../Utils/TypeHelpers";
import usePromise from "react-promise-suspense";
import { delay } from "../Utils/AsyncUtils";

// eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-require-imports
const hardCodedGroups: GroupNode[] = require("../../gitlab-projects.json");

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
    /* eslint-disable @typescript-eslint/no-unsafe-return */
    return usePromise(async () => {
        await delay(100);
        return (
            hardCodedGroups.find(
                x => x.id === groupIdOrTitle || x.title.toLowerCase() === groupIdOrTitle.toLowerCase()
            ) ?? reject(`Unable to find group ${groupIdOrTitle}`)
        );
    }, ["hardCodedGroups", groupIdOrTitle]);
}

export function useRootGroups(): Group[] {
    /* eslint-disable @typescript-eslint/no-unsafe-return */
    return usePromise(async () => {
        await delay(100);
        return hardCodedGroups.map(x => ({ id: x.id, title: x.title }));
    }, ["rootGroups"]);
}
