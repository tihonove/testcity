import {
    EyeClosedIcon16Regular,
    EyeOpenIcon16Regular,
    PlusCircleIcon16Solid,
    TextBulletIcon20Regular,
    TransportAirRocketIcon16Light,
    UiMenuShapeCircle4Icon20Light,
    UiMenuShapeCircle4Icon24Regular,
    UiMenuShapeSquare4TiltIcon24Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Button, Hint, Link as ReactUILink } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchSelect } from "../Components/BranchSelect";
import { GroupAvatar } from "../Components/GroupAvatar";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { LogoPageBlock } from "../Components/LogoPageBlock";
import { ManualJobsInfo } from "../Components/ManualJobsInfo";
import { ProjectItem, ProjectItemProps } from "../Components/ProjectItem";
import { SubIcon } from "../Components/SubIcon";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { JobIdWithParentProject } from "../Domain/JobIdWithParentProject";
import { createLinkToCreateNewPipeline, createLinkToGroupOrProject, createLinkToProject } from "../Domain/Navigation";
import { JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { PipelineRunsQueryRow } from "../Domain/Storage/PipelineRunsQueryRow";
import { GroupNode, Project, getProjects, isGroup, isProject } from "../Domain/Storage/Projects/GroupNode";
import { useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { ProjectsWithRunsTable } from "./ProjectsWithRunsTable";

import styles from "./ProjectsDashboardPage.module.css";

export function ProjectsDashboardPage(): React.JSX.Element {
    const { rootGroup, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const [isPending, startTransition, isFading] = useDelayedTransition();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);
    const [noRuns, setNoRuns] = useSearchParamAsState("noruns");

    const usePipelineGrouping = rootGroup.mergeRunsFromJobs ?? true;
    const currentGroupOrProject = groupNodes[groupNodes.length - 1];
    const projects = getProjects(currentGroupOrProject);
    const projectIds = React.useMemo(() => projects.map(p => p.id), [projects]);

    const allJobs = useStorageQuery(x => x.findAllJobs(projectIds), [projectIds]);
    const inProgressJobRuns = useStorageQuery(
        x => (usePipelineGrouping ? [] : x.findAllJobsRunsInProgress(projectIds, currentBranchName)),
        [projectIds, currentBranchName, usePipelineGrouping, pathToGroup]
    );
    const allJobRuns2 = useStorageQuery(
        x => (usePipelineGrouping ? [] : x.findAllJobsRuns(projectIds, currentBranchName)),
        [projectIds, currentBranchName, usePipelineGrouping, pathToGroup]
    );
    const allJobRuns = [...inProgressJobRuns, ...allJobRuns2];

    const allPipelineRuns = useStorageQuery(
        x =>
            usePipelineGrouping
                ? isProject(currentGroupOrProject)
                    ? x.getPipelineRunsByProject(currentGroupOrProject.id, currentBranchName)
                    : x.getPipelineRunsOverview(projectIds, currentBranchName)
                : [],
        [projectIds, currentBranchName, usePipelineGrouping, pathToGroup]
    );

    const renderProject = (project: Project, level: number, nodes: (GroupNode | Project)[]) => (
        <ProjectItem
            key={project.id}
            project={project}
            level={level}
            nodes={nodes}
            currentGroupOrProject={currentGroupOrProject}
            rootGroup={rootGroup}
            currentBranchName={currentBranchName}
            usePipelineGrouping={usePipelineGrouping}
            allPipelineRuns={allPipelineRuns}
            allJobs={allJobs}
            allJobRuns={allJobRuns}
            noRuns={noRuns}
        />
    );

    const renderGroupList = (groups: GroupNode[], nodesPath: GroupNode[], level: number) => {
        return (
            <>
                {groups.map(x => (
                    <GroupItem
                        key={x.id}
                        group={x}
                        nodesPath={[...nodesPath, x]}
                        level={level}
                        currentBranchName={currentBranchName}
                        renderGroupList={renderGroupList}
                        currentGroupOrProject={currentGroupOrProject}
                        rootGroup={rootGroup}
                        usePipelineGrouping={usePipelineGrouping}
                        allPipelineRuns={allPipelineRuns}
                        allJobs={allJobs}
                        allJobRuns={allJobRuns}
                        noRuns={noRuns}
                    />
                ))}
            </>
        );
    };

    return (
        <main className={styles.root}>
            <LogoPageBlock />
            <div className={styles.testListRoot}>
                <ColumnStack block gap={1} stretch>
                    <Fit>
                        <ColumnStack block gap={1} stretch>
                            <Fit nextGap={4}>
                                <GroupBreadcrumps branchName={currentBranchName} nodes={groupNodes.slice(0, -1)} />
                            </Fit>
                            <Fit>
                                <RowStack gap={2} block verticalAlign="center">
                                    <Fit>
                                        {isProject(currentGroupOrProject) ? (
                                            <UiMenuShapeSquare4TiltIcon24Regular />
                                        ) : (
                                            <UiMenuShapeCircle4Icon24Regular />
                                        )}
                                    </Fit>
                                    <Fit>
                                        <GroupAvatar size="32px" group={currentGroupOrProject}></GroupAvatar>
                                    </Fit>
                                    <Fit>
                                        <h1 className={styles.rootGroupTitle}>{currentGroupOrProject.title}</h1>
                                    </Fit>
                                    {isProject(currentGroupOrProject) && (
                                        <>
                                            <Fill />
                                            <Fit style={{ minHeight: "35px" }}>
                                                <ManualJobsInfo
                                                    projectId={currentGroupOrProject.id}
                                                    allPipelineRuns={allPipelineRuns}
                                                />
                                            </Fit>
                                            <Fit>
                                                <Hint text="Create new pipeline">
                                                    <ReactUILink
                                                        href={createLinkToCreateNewPipeline(
                                                            rootGroup,
                                                            currentGroupOrProject.id,
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
                            onChangeBranch={x => {
                                startTransition(() => {
                                    setCurrentBranchName(x);
                                });
                            }}
                            projectIds={projectIds}
                        />
                    </Fit>
                    <Fit>
                        <SuspenseFadingWrapper fading={isFading}>
                            <ProjectsWithRunsTable>
                                {(isGroup(currentGroupOrProject) ? (currentGroupOrProject.projects ?? []) : []).map(x =>
                                    renderProject(x, 0, groupNodes)
                                )}
                                {isGroup(currentGroupOrProject) &&
                                    renderGroupList(
                                        currentGroupOrProject.groups ?? [],
                                        groupNodes.filter(x => isGroup(x)),
                                        0
                                    )}
                                {isProject(currentGroupOrProject) &&
                                    renderProject(currentGroupOrProject, 0, groupNodes)}
                            </ProjectsWithRunsTable>
                        </SuspenseFadingWrapper>
                    </Fit>
                </ColumnStack>
            </div>
            {!usePipelineGrouping && (
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
            )}
        </main>
    );
}

interface GroupItemProps {
    group: GroupNode;
    nodesPath: GroupNode[];
    level: number;
    currentBranchName: string | undefined;
    renderGroupList: (groups: GroupNode[], nodesPath: GroupNode[], level: number) => React.JSX.Element;
    // Props for ProjectItem
    currentGroupOrProject: GroupNode | Project;
    rootGroup: GroupNode;
    usePipelineGrouping: boolean;
    allPipelineRuns: PipelineRunsQueryRow[];
    allJobs: JobIdWithParentProject[];
    allJobRuns: JobsQueryRow[];
    noRuns: string | undefined;
}

function GroupItem({
    group,
    nodesPath,
    level,
    currentBranchName,
    renderGroupList,
    currentGroupOrProject,
    rootGroup,
    usePipelineGrouping,
    allPipelineRuns,
    allJobs,
    allJobRuns,
    noRuns,
}: GroupItemProps): React.JSX.Element {
    const [collapsed, setCollapsed] = React.useState(false);

    return (
        <React.Fragment key={group.id}>
            <thead>
                <tr>
                    <td colSpan={5} style={{ paddingLeft: 25 * level }}>
                        <div className={styles.sectionTitle}>
                            <RowStack gap={2} baseline block>
                                <Fit>
                                    <UiMenuShapeCircle4Icon20Light />
                                </Fit>
                                <Fit>
                                    <GroupAvatar size="20px" group={group}></GroupAvatar>
                                </Fit>
                                <Fit>
                                    <Link to={createLinkToGroupOrProject(nodesPath, currentBranchName)}>
                                        <h3 className={styles.header3}>{group.title}</h3>
                                    </Link>
                                </Fit>
                            </RowStack>
                        </div>
                    </td>
                </tr>
            </thead>
            {(group.projects ?? []).map(x => (
                <ProjectItem
                    key={x.id}
                    project={x}
                    level={level + 1}
                    nodes={[...nodesPath, x]}
                    currentGroupOrProject={currentGroupOrProject}
                    rootGroup={rootGroup}
                    currentBranchName={currentBranchName}
                    usePipelineGrouping={usePipelineGrouping}
                    allPipelineRuns={allPipelineRuns}
                    allJobs={allJobs}
                    allJobRuns={allJobRuns}
                    noRuns={noRuns}
                />
            ))}
            {renderGroupList(group.groups ?? [], [...nodesPath, group], level + 1)}
        </React.Fragment>
    );
}


