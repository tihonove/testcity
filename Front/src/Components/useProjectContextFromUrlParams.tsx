import * as React from "react";
import { useParams } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { GroupNode, Project } from "../Domain/Storage/Storage";
import { reject } from "../TypeHelpers";

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
    const rootProjectStructure = useStorageQuery(x => x.getRootProjectStructure(groupIdLevel1), [groupIdLevel1]);
    const groupNodes = useStorageQuery(
        x => x.resolvePathToNodes(pathToGroup) ?? reject("Undefined is no allowed"),
        [pathToGroup]
    );

    return {
        rootGroup: rootProjectStructure,
        groupNodes: groupNodes,
        pathToGroup: pathToGroup,
    };
}
