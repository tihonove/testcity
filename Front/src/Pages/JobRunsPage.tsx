import { ShapeSquareIcon32Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { Tabs } from "@skbkontur/react-ui";
import * as React from "react";
import { useParams } from "react-router-dom";
import { BranchSelect } from "../Components/BranchSelect";
import { FlakyTestsList } from "../Components/FlakyTestsList";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { reject } from "../Utils/TypeHelpers";
import styles from "./JobRunsPage.module.css";
import { JobRunList } from "./JobRunList";

export function JobRunsPage(): React.JSX.Element {
    const { jobId = "" } = useParams();
    const { groupNodes, rootGroup, pathToGroup } = useProjectContextFromUrlParams();
    const project = groupNodes[groupNodes.length - 1] ?? reject("Project not found");
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);

    const [section, setSection] = useSearchParamAsState("section", "overview");

    return (
        <ColumnStack block stretch gap={4}>
            <Fit>
                <GroupBreadcrumps branchName={currentBranchName} nodes={groupNodes} />
            </Fit>
            <Fit>
                <h1 className={styles.header1}>
                    <ShapeSquareIcon32Regular /> {jobId}
                </h1>
            </Fit>
            <Fit>
                <BranchSelect branch={currentBranchName} onChangeBranch={setCurrentBranchName} jobId={jobId} />
            </Fit>
            <Fit>
                <Tabs value={section ?? "overview"} onValueChange={setSection}>
                    <Tabs.Tab id="overview">Overview</Tabs.Tab>
                    <Tabs.Tab id="statistics">Statistics</Tabs.Tab>
                    <Tabs.Tab id="flaky-tests">Flaky Tests</Tabs.Tab>
                </Tabs>
            </Fit>
            <Fit>
                {section === "overview" && (
                    <JobRunList project={project} jobId={jobId} branchName={currentBranchName} rootGroup={rootGroup} />
                )}
                {section === "statistics" && (
                    <div>
                        {/* TODO: Добавить содержимое для вкладки Statistics */}
                        <p>Statistics content coming soon...</p>
                    </div>
                )}
                {section === "flaky-tests" && (
                    <FlakyTestsList pathToProject={pathToGroup} projectId={project.id} jobId={jobId} />
                )}
            </Fit>
        </ColumnStack>
    );
}
