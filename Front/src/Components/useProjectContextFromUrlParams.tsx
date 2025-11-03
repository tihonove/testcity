import * as React from "react";
import { useParams } from "react-router-dom";
import { GroupNode, Project, resolvePathToNodes } from "../Domain/Storage/Projects/GroupNode";
import { reject } from "../Utils/TypeHelpers";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";

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
    const rootProjectStructure = useTestCityRequest(c => c.getRootGroup(groupIdLevel1), [groupIdLevel1]);
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
