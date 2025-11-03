import {
    ArrowShapeTriangleADownIcon20Light,
    ArrowShapeTriangleARightIcon20Light,
    PlusCircleIcon16Solid,
    TransportAirRocketIcon16Light,
    UiMenuShapeSquare4TiltIcon20Light,
} from "@skbkontur/icons";
import { Fill, Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import { Hint, Link as ReactUILink } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { GroupAvatar } from "../../../Components/GroupAvatar";
import { JobsView } from "../../../Components/JobsView";
import { ManualJobsInfo } from "../../../Components/ManualJobsInfo";
import { SubIcon } from "../../../Components/SubIcon";
import { createLinkToCreateNewPipeline2 } from "../../../Domain/Navigation";
import { RunsTable } from "./ProjectsWithRunsTable";

import { ProjectDashboardNode } from "../../../Domain/ProjectDashboardNode";
import { useUserSettings } from "../../../Utils/useUserSettings";
import styles from "./ProjectItemDashboardTable.module.css";

export interface ProjectItemProps {
    project: ProjectDashboardNode;
    level: number;
    currentBranchName: string | undefined;
    noRuns: string | undefined;
    doNotRenderHeader?: boolean;
}

export function ProjectItemDashboardTable({
    project,
    level,
    currentBranchName,
    noRuns,
    doNotRenderHeader,
}: ProjectItemProps): React.JSX.Element {
    const [collapsed, setCollapsed] = useUserSettings(
        ["ui", ...[...project.fullPathSlug].map(n => n.id), "collapsed"],
        false
    );

    return (
        <React.Fragment key={project.id}>
            {!doNotRenderHeader && (
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
                                            to={
                                                project.link + (currentBranchName ? `?branch=${currentBranchName}` : "")
                                            }>
                                            <h3 className={styles.header3}>{project.title}</h3>
                                        </Link>
                                    </Fit>
                                    <Fill />
                                    <Fit style={{ minHeight: "35px" }}>
                                        <ManualJobsInfo
                                            key={project.id + (currentBranchName ?? "all")}
                                            projectId={project.id}
                                        />
                                    </Fit>
                                    <Fit>
                                        <Hint text="Create new pipeline">
                                            <ReactUILink
                                                href={createLinkToCreateNewPipeline2(
                                                    project.gitLabLink,
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
            {(!collapsed || doNotRenderHeader) && (
                <JobsView
                    project={project}
                    jobs={project.jobs}
                    indentLevel={level}
                    hideRuns={noRuns === "1"}
                    currentBranchName={currentBranchName}
                />
            )}
        </React.Fragment>
    );
}
