import {
    ArrowShapeTriangleADownIcon20Light,
    ArrowShapeTriangleARightIcon20Light,
    PlusCircleIcon16Solid,
    TransportAirRocketIcon16Light,
    UiMenuShapeSquare4TiltIcon20Light,
} from "@skbkontur/icons";
import { Fill, Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import { Button, Hint, Link as ReactUILink } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { useLocalStorage } from "usehooks-ts";
import { GroupAvatar } from "./GroupAvatar";
import { JobsView } from "./JobsView";
import { ManualJobsInfo } from "./ManualJobsInfo";
import { PipelineRuns } from "./PipelineRuns";
import { SubIcon } from "./SubIcon";
import { JobIdWithParentProjectNames, JobIdWithParentProject } from "../Domain/JobIdWithParentProject";
import { createLinkToCreateNewPipeline, createLinkToProject } from "../Domain/Navigation";
import { JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { PipelineRunsQueryRow } from "../Domain/Storage/PipelineRunsQueryRow";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { RunsTable } from "../Pages/ProjectsWithRunsTable";

import styles from "./ProjectItem.module.css";
import { useUserSettings } from "../Utils/useUserSettings";

export interface ProjectItemProps {
    project: Project;
    level: number;
    nodes: (GroupNode | Project)[];
    currentGroupOrProject: GroupNode | Project;
    rootGroup: GroupNode;
    currentBranchName: string | undefined;
    usePipelineGrouping: boolean;
    allPipelineRuns: PipelineRunsQueryRow[];
    allJobs: JobIdWithParentProject[];
    allJobRuns: JobsQueryRow[];
    noRuns: string | undefined;
}

export function ProjectItem({
    project,
    level,
    nodes,
    currentGroupOrProject,
    rootGroup,
    currentBranchName,
    usePipelineGrouping,
    allPipelineRuns,
    allJobs,
    allJobRuns,
    noRuns,
}: ProjectItemProps): React.JSX.Element {
    const [collapsed, setCollapsed] = useUserSettings(["collapsed", nodes.map(n => n.id).join(".")], false);

    return (
        <React.Fragment key={project.id}>
            {project !== currentGroupOrProject && (
                <thead>
                    <tr>
                        <td colSpan={RunsTable.columnCount} style={{ paddingLeft: 25 * level }}>
                            <div className={styles.sectionTitle}>
                                <RowStack gap={2} baseline block>
                                    <Fixed width={20}>
                                        <button
                                            className={styles.iconButton}
                                            type="button"
                                            onClick={() => {
                                                setCollapsed(!collapsed);
                                                return false;
                                            }}>
                                            {collapsed ? (
                                                <ArrowShapeTriangleARightIcon20Light />
                                            ) : (
                                                <ArrowShapeTriangleADownIcon20Light />
                                            )}
                                        </button>
                                    </Fixed>
                                    <Fit>
                                        <UiMenuShapeSquare4TiltIcon20Light />
                                    </Fit>
                                    <Fit>
                                        <GroupAvatar size="20px" group={project}></GroupAvatar>
                                    </Fit>
                                    <Fit>
                                        <Link
                                            className="no-underline"
                                            to={createLinkToProject(rootGroup, project.id, currentBranchName)}>
                                            <h3 className={styles.header3}>{project.title}</h3>
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
                                                    rootGroup,
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
                                </RowStack>
                            </div>
                        </td>
                    </tr>
                </thead>
            )}
            {!collapsed && (
                <>
                    {usePipelineGrouping ? (
                        <PipelineRuns
                            groupNodes={nodes}
                            indentLevel={level}
                            project={project}
                            currentBranchName={currentBranchName}
                            rootProjectStructure={rootGroup}
                            allPipelineRuns={allPipelineRuns}
                        />
                    ) : (
                        <JobsView
                            indentLevel={level}
                            hideRuns={noRuns === "1"}
                            currentBranchName={currentBranchName}
                            rootProjectStructure={rootGroup}
                            allJobs={allJobs.filter(j => j[JobIdWithParentProjectNames.ProjectId] === project.id)}
                            allJobRuns={allJobRuns}
                        />
                    )}
                </>
            )}
        </React.Fragment>
    );
}
