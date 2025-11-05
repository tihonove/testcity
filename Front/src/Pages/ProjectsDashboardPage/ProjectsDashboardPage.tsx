import {
    EyeClosedIcon16Regular,
    EyeOpenIcon16Regular,
    PlusCircleIcon16Solid,
    TextBulletIcon20Regular,
    TransportAirRocketIcon16Light,
    UiMenuShapeCircle4Icon24Regular,
    UiMenuShapeSquare4TiltIcon24Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Button, Hint, Link as ReactUILink } from "@skbkontur/react-ui";
import * as React from "react";
import { BranchSelect } from "../../Components/BranchSelect";
import { GroupAvatar } from "../../Components/GroupAvatar";
import { GroupBreadcrumps } from "../../Components/GroupBreadcrumps";
import { LogoPageBlock } from "../../Components/LogoPageBlock";
import { ManualJobsInfo } from "../../Components/ManualJobsInfo";
import { SubIcon } from "../../Components/SubIcon";
import { SuspenseFadingWrapper, useDelayedTransition } from "../../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../../Components/useProjectContextFromUrlParams";
import { createLinkToCreateNewPipeline2 } from "../../Domain/Navigation";
import { useSearchParamAsState } from "../../Utils";
import { usePopularBranchStoring } from "../../Utils/PopularBranchStoring";
import { GroupItemDashboardTable } from "./Components/GroupItemDashboardTable";
import { ProjectItemDashboardTable } from "./Components/ProjectItemDashboardTable";
import { ProjectsWithRunsTable } from "./Components/ProjectsWithRunsTable";

import styles from "./ProjectsDashboardPage.module.css";
import { useTestCityRequest } from "../../Domain/Api/TestCityApiClient";

export function ProjectsDashboardPage(): React.JSX.Element {
    const { rootGroup, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const [isPending, startTransition, isFading] = useDelayedTransition();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);
    const [noRuns, setNoRuns] = useSearchParamAsState("noruns");

    const dashboardData = useTestCityRequest(
        c =>
            c.runs.getDashboard(
                groupNodes.map(x => x.title),
                currentBranchName
            ),
        [...groupNodes.map(x => x.title), currentBranchName]
    );

    return (
        <main className={styles.root}>
            <LogoPageBlock />
            <div className={styles.testListRoot}>
                <ColumnStack block gap={1} stretch>
                    <Fit>
                        <ColumnStack block gap={1} stretch>
                            <Fit nextGap={4}>
                                <GroupBreadcrumps
                                    branchName={currentBranchName}
                                    nodes={dashboardData.fullPathSlug.slice(0, -1)}
                                />
                            </Fit>
                            <Fit>
                                <RowStack gap={2} block verticalAlign="center">
                                    <Fit>
                                        {dashboardData.type == "project" ? (
                                            <UiMenuShapeSquare4TiltIcon24Regular />
                                        ) : (
                                            <UiMenuShapeCircle4Icon24Regular />
                                        )}
                                    </Fit>
                                    <Fit>
                                        <GroupAvatar size="32px" group={dashboardData}></GroupAvatar>
                                    </Fit>
                                    <Fit>
                                        <h1 className={styles.rootGroupTitle}>{dashboardData.title}</h1>
                                    </Fit>
                                    {dashboardData.type == "project" && (
                                        <>
                                            <Fill />
                                            <Fit style={{ minHeight: "35px" }}>
                                                <ManualJobsInfo projectId={dashboardData.id} />
                                            </Fit>
                                            <Fit>
                                                <Hint text="Create new pipeline">
                                                    <ReactUILink
                                                        href={createLinkToCreateNewPipeline2(
                                                            dashboardData.gitLabLink,
                                                            currentBranchName
                                                        )}
                                                        target="_blank"
                                                        icon={
                                                            <SubIcon sub={<PlusCircleIcon16Solid />}>
                                                                <TransportAirRocketIcon16Light />
                                                            </SubIcon>
                                                        }>
                                                        New pipeline
                                                    </ReactUILink>
                                                </Hint>
                                            </Fit>
                                        </>
                                    )}
                                </RowStack>
                            </Fit>
                        </ColumnStack>
                    </Fit>
                    <Fit>
                        <div style={{ height: 20 }} />
                    </Fit>
                    <Fit>
                        <BranchSelect
                            branch={currentBranchName}
                            pathToGroup={pathToGroup}
                            onChangeBranch={x => {
                                startTransition(() => {
                                    setCurrentBranchName(x);
                                });
                            }}
                        />
                    </Fit>
                    <Fit>
                        <SuspenseFadingWrapper fading={isFading}>
                            <ProjectsWithRunsTable>
                                {dashboardData.type == "group" && (
                                    <GroupItemDashboardTable
                                        group={dashboardData}
                                        level={0}
                                        currentBranchName={currentBranchName}
                                        noRuns={noRuns}
                                        doNotRenderHeader
                                    />
                                )}
                                {dashboardData.type == "project" && (
                                    <ProjectItemDashboardTable
                                        project={dashboardData}
                                        level={0}
                                        currentBranchName={currentBranchName}
                                        noRuns={noRuns}
                                        doNotRenderHeader
                                    />
                                )}
                            </ProjectsWithRunsTable>
                        </SuspenseFadingWrapper>
                    </Fit>
                </ColumnStack>
            </div>
            <div className={styles.showRunsSwitchContainer}>
                <Button
                    use="link"
                    onClick={() => {
                        setNoRuns(noRuns === "1" ? undefined : "1");
                    }}>
                    <SubIcon sub={noRuns ? <EyeClosedIcon16Regular /> : <EyeOpenIcon16Regular />}>
                        <TextBulletIcon20Regular />
                    </SubIcon>
                </Button>
            </div>
        </main>
    );
}
