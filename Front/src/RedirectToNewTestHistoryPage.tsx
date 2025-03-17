import * as React from "react";
import { useNavigate } from "react-router-dom";
import { useStorageQuery } from "./ClickhouseClientHooksWrapper";
import { useBasePrefix, createLinkToTestHistory } from "./Domain/Navigation";
import { useSearchParam } from "./Utils";

export function RedirectToNewTestHistoryPage() {
    const basePrefix = useBasePrefix();
    const [testId] = useSearchParam("id");
    if (testId == null) throw new Error("Test id is not defined");
    const [currentJobId] = useSearchParam("job");
    const currentProjectId = useStorageQuery(x => x.findProjectByTestId(testId, currentJobId), [testId, currentJobId]);
    if (currentProjectId == null) throw new Error("Project not found");
    const path = useStorageQuery(x => x.findPathToProjectByIdOrNull(currentProjectId), [currentProjectId]);
    if (path == null) throw new Error("Project not found");
    const navigate = useNavigate();

    React.useEffect(() => {
        const url = createLinkToTestHistory(basePrefix, testId, [...path[0].map(x => x.title), path[1].title]);
        navigate(url, { replace: true });
    }, [testId, path, navigate]);

    return <div>Redirecting to {[...path[0].map(x => x.title), path[1].title].join("/")}</div>;
}
