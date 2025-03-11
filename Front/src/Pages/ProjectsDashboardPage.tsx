import {
    EyeClosedIcon16Regular,
    EyeOpenIcon16Regular,
    PlusCircleIcon16Solid,
    TextBulletIcon20Regular,
    TextBulletIcon24Regular,
    TransportAirRocketIcon16Light,
    TransportAirRocketIcon20Light,
    UiMenuShapeCircle4Icon20Light,
    UiMenuShapeCircle4Icon24Regular,
    UiMenuShapeSquare4TiltIcon20Light,
    UiMenuShapeSquare4TiltIcon24Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Button, Hint, Link as ReactUILink } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchSelect } from "../Components/BranchSelect";
import { GroupAvatar } from "../Components/GroupAvatar";
import { SubIcon } from "../Components/SubIcon";
import { JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobsView } from "../Domain/JobsView";
import { createLinkToCreateNewPipeline, createLinkToGroupOrProject, createLinkToProject } from "../Domain/Navigation";
import { PipelineRuns } from "../Domain/PipelineRuns";
import { GroupNode, isGroup, isProject, Project } from "../Domain/Storage";
import { theme } from "../Theme/ITheme";
import { useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { GroupBreadcrumps } from "./GroupBreadcrumps";
import { useProjectContextFromUrlParams } from "./useProjectContextFromUrlParams";
import { SuspenseFadingWrapper, useDelayedTransition } from "./useDelayedTransition";
import { ManualJobsInfo } from "../Domain/ManualJobsInfo";
import { LogoPageBlock } from "./LogoPageBlock";

export function ProjectsDashboardPage(): React.JSX.Element {
    const { rootGroup: rootProjectStructure, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const [isPending, startTransition, isFading] = useDelayedTransition();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);
    const [noRuns, setNoRuns] = useSearchParamAsState("noruns");

    const usePipelineGrouping = rootProjectStructure.mergeRunsFromJobs ?? true;
    const currentGroupOrProject = groupNodes[groupNodes.length - 1];
    const projects = useStorageQuery(x => x.getProjects(pathToGroup), [pathToGroup]);
    const projectIds = React.useMemo(() => projects.map(p => p.id), [projects]);
    const allJobs = useStorageQuery(x => x.findAllJobs(projectIds), [projectIds]);

    const allJobRuns = useStorageQuery(
        x => (usePipelineGrouping ? [] : x.findAllJobsRuns(projectIds, currentBranchName)),
        [projectIds, currentBranchName, usePipelineGrouping, pathToGroup]
    );
    const allPipelineRuns = useStorageQuery(
        x =>
            usePipelineGrouping && isProject(currentGroupOrProject)
                ? x.getPipelineRunsByProject(currentGroupOrProject.id, currentBranchName)
                : usePipelineGrouping
                  ? x.getPipelineRunsOverview(projectIds, currentBranchName)
                  : [],
        [projectIds, currentBranchName, usePipelineGrouping, pathToGroup]
    );

    const renderProject = (project: Project, level: number) => (
        <React.Fragment key={project.id}>
            {project !== currentGroupOrProject && (
                <thead>
                    <tr>
                        <td colSpan={6} style={{ paddingLeft: 25 * level }}>
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
                    hideRuns={noRuns === "1"}
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

const ProjectsWithRunsTable = styled.table`
    width: 100%;
    font-size: 14px;

    td {
        padding: 6px 8px;
    }

    thead > tr > th {
        padding-top: 16px;
    }
`;

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

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
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
    display: block;
    font-size: 32px;
    line-height: 40px;
    text-decoration: none;

    :hover {
        background-color: ${theme.backgroundColor1};
    }
`;
