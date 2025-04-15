import {
    EyeClosedIcon16Regular,
    EyeOpenIcon16Regular,
    PlusCircleIcon16Solid,
    TextBulletIcon20Regular,
    TransportAirRocketIcon16Light,
    UiMenuShapeCircle4Icon20Light,
    UiMenuShapeCircle4Icon24Regular,
    UiMenuShapeSquare4TiltIcon20Light,
    UiMenuShapeSquare4TiltIcon24Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Button, Hint, Link as ReactUILink } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { styled } from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchSelect } from "../Components/BranchSelect";
import { GroupAvatar } from "../Components/GroupAvatar";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { LogoPageBlock } from "../Components/LogoPageBlock";
import { SubIcon } from "../Components/SubIcon";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobsView } from "../Components/JobsView";
import { ManualJobsInfo } from "../Components/ManualJobsInfo";
import { createLinkToCreateNewPipeline, createLinkToGroupOrProject, createLinkToProject } from "../Domain/Navigation";
import { PipelineRuns } from "../Components/PipelineRuns";
import { GroupNode, isGroup, isProject, Project } from "../Domain/Storage/Storage";
import { theme } from "../Theme/ITheme";
import { useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { ProjectsWithRunsTable, RunsTable } from "./ProjectsWithRunsTable";
import { useShowChangesFeature } from "./useShowChangesFeature";
import { JobRunNames } from "../Domain/Storage/JobsQuery";

export function ProjectsDashboardPage(): React.JSX.Element {
    const { rootGroup: rootProjectStructure, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const [isPending, startTransition, isFading] = useDelayedTransition();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);
    const [noRuns, setNoRuns] = useSearchParamAsState("noruns");

    const showChanges = useShowChangesFeature();
    const usePipelineGrouping = rootProjectStructure.mergeRunsFromJobs ?? true;
    const currentGroupOrProject = groupNodes[groupNodes.length - 1];
    const projects = useStorageQuery(x => x.getProjects(pathToGroup), [pathToGroup]);
    const projectIds = React.useMemo(() => projects.map(p => p.id), [projects]);

    const allJobs = useStorageQuery(x => x.findAllJobs(projectIds), [projectIds]);
    const inProgressJobRuns = useStorageQuery(
        x => (usePipelineGrouping ? [] : showChanges ? [] : x.findAllJobsRunsInProgress(projectIds, currentBranchName)),
        [projectIds, currentBranchName, usePipelineGrouping, pathToGroup]
    );
    // const inProgressJobRuns = [
    //     [
    //         "DotNet tests",
    //         "38995054",
    //         "main",
    //         "agent_12616",
    //         "2025-04-15 20:49:41",
    //         null,
    //         "linux",
    //         null,
    //         null,
    //         null,
    //         null,
    //         "Running",
    //         null,
    //         "https://git.skbkontur.ru/forms/test-analytics/-/jobs/38995054",
    //         "24783",
    //         0,
    //     ],
    // ];
    const allJobRuns2 = useStorageQuery(
        x =>
            usePipelineGrouping
                ? []
                : showChanges
                  ? x.findAllJobsRunsWithChanges(projectIds, currentBranchName)
                  : x.findAllJobsRuns(projectIds, currentBranchName),
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

    const renderProject = (project: Project, level: number) => (
        <React.Fragment key={project.id}>
            {project !== currentGroupOrProject && (
                <thead>
                    <tr>
                        <td colSpan={RunsTable.columnCount} style={{ paddingLeft: 25 * level }}>
                            <SectionTitle gap={2} baseline block>
                                <Fit>
                                    <UiMenuShapeSquare4TiltIcon20Light />
                                </Fit>
                                <Fit>
                                    <GroupAvatar size="20px" group={project}></GroupAvatar>
                                </Fit>
                                <Fit>
                                    <Link
                                        className="no-underline"
                                        to={createLinkToProject(rootProjectStructure, project.id, currentBranchName)}>
                                        <Header3>{project.title}</Header3>
                                    </Link>
                                </Fit>
                                <Fill />
                                <Fit style={{ minHeight: "35px" }}>
                                    <ManualJobsInfo
                                        key={project.id + (currentBranchName ?? "all")}
                                        projectId={project.id}
                                        allPipelineRuns={allPipelineRuns}
                                    />
                                </Fit>
                                <Fit>
                                    <Hint text="Create new pipeline">
                                        <ReactUILink
                                            href={createLinkToCreateNewPipeline(
                                                rootProjectStructure,
                                                project.id,
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
                            </SectionTitle>
                        </td>
                    </tr>
                </thead>
            )}
            {usePipelineGrouping ? (
                <PipelineRuns
                    indentLevel={level}
                    project={project}
                    currentBranchName={currentBranchName}
                    rootProjectStructure={rootProjectStructure}
                    allPipelineRuns={allPipelineRuns}
                />
            ) : (
                <JobsView
                    indentLevel={level}
                    hideRuns={noRuns === "1"}
                    currentBranchName={currentBranchName}
                    rootProjectStructure={rootProjectStructure}
                    allJobs={allJobs.filter(j => j[JobIdWithParentProjectNames.ProjectId] === project.id)}
                    allJobRuns={allJobRuns}
                />
            )}
        </React.Fragment>
    );

    const renderGroupList = (groups: GroupNode[], nodesPath: GroupNode[], level: number) => {
        return <>{groups.map(x => renderGroup(x, [...nodesPath, x], level))}</>;
    };

    const renderGroup = (group: GroupNode, nodesPath: GroupNode[], level: number) => {
        return (
            <React.Fragment key={group.id}>
                <thead>
                    <tr>
                        <td colSpan={5} style={{ paddingLeft: 25 * level }}>
                            <SectionTitle gap={2} baseline block>
                                <Fit>
                                    <UiMenuShapeCircle4Icon20Light />
                                </Fit>
                                <Fit>
                                    <GroupAvatar size="20px" group={group}></GroupAvatar>
                                </Fit>
                                <Fit>
                                    <Link
                                        className="no-underline"
                                        to={createLinkToGroupOrProject(nodesPath, currentBranchName)}>
                                        <Header3>{group.title}</Header3>
                                    </Link>
                                </Fit>
                            </SectionTitle>
                        </td>
                    </tr>
                </thead>
                {(group.projects ?? []).map(x => renderProject(x, level + 1))}
                {renderGroupList(group.groups ?? [], [...nodesPath, group], level + 1)}
            </React.Fragment>
        );
    };

    return (
        <Root>
            <LogoPageBlock />
            <TestListRoot block gap={1} stretch>
                <Fit>
                    <ColumnStack block gap={1} stretch>
                        <Fit>
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
                                    <RootGroupTitle>{currentGroupOrProject.title}</RootGroupTitle>
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
                                                        rootProjectStructure,
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
                    <SuspenseFadingWrapper $fading={isFading}>
                        <ProjectsWithRunsTable>
                            {(isGroup(currentGroupOrProject) ? (currentGroupOrProject.projects ?? []) : []).map(x =>
                                renderProject(x, 0)
                            )}
                            {isGroup(currentGroupOrProject) &&
                                renderGroupList(
                                    currentGroupOrProject.groups ?? [],
                                    groupNodes.filter(x => isGroup(x)),
                                    0
                                )}
                            {isProject(currentGroupOrProject) && renderProject(currentGroupOrProject, 0)}
                        </ProjectsWithRunsTable>
                    </SuspenseFadingWrapper>
                </Fit>
            </TestListRoot>
            {!usePipelineGrouping && (
                <ShowRunsSwitchContainer>
                    <Button
                        use="link"
                        onClick={() => {
                            setNoRuns(noRuns === "1" ? undefined : "1");
                        }}>
                        <SubIcon sub={noRuns ? <EyeClosedIcon16Regular /> : <EyeOpenIcon16Regular />}>
                            <TextBulletIcon20Regular />
                        </SubIcon>
                    </Button>
                </ShowRunsSwitchContainer>
            )}
        </Root>
    );
}

const TestListRoot = styled(ColumnStack)`
    max-width: ${theme.layout.centered.width};
    width: ${theme.layout.centered.width};
    margin: 24px auto;
`;

const SectionTitle = styled(RowStack)`
    margin-top: 16px;
`;

const Root = styled.main`
    display: flex;
`;

const ShowRunsSwitchContainer = styled.div`
    position: fixed;
    right: 40px;
    top: 10px;
`;

const Header3 = styled.h3`
    font-size: 22px;
    line-height: 24px;
`;

const RootGroupTitle = styled.h1`
    ${theme.typography.pages.header1};
    display: block;
    text-decoration: none;

    :hover {
        background-color: ${theme.backgroundColor1};
    }
`;
