import * as React from "react";
import { useParams } from "react-router-dom";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";
import { GroupEntityNode, ProjectEntityNode } from "../Domain/Api/TestCityRunsApiClient";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";

export function useProjectContextFromUrlParams(): {
    rootGroup: ProjectEntityNode | GroupEntityNode;
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
    const result = useTestCityRequest(c => c.runs.getEntity(pathToGroup), [pathToGroup]);

    return {
        rootGroup: result,
        pathToGroup: pathToGroup,
    };
}
