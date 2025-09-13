import {
    ArrowShapeTriangleADownIcon20Light,
    ArrowShapeTriangleARightIcon20Light,
    UiMenuShapeCircle4Icon20Light,
} from "@skbkontur/icons";
import { Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link } from "react-router-dom";
import { GroupAvatar } from "../../../Components/GroupAvatar";
import { ProjectItemDashboardTable } from "./ProjectItemDashboardTable";
import { createLinkToGroupOrProject } from "../../../Domain/Navigation";
import { JobIdWithParentProject } from "../../../Domain/JobIdWithParentProject";
import { JobsQueryRow } from "../../../Domain/Storage/JobsQuery";
import { PipelineRunsQueryRow } from "../../../Domain/Storage/PipelineRunsQueryRow";
import { GroupNode, Project } from "../../../Domain/Storage/Projects/GroupNode";

import styles from "./GroupItemDashboardTable.module.css";
import { useUserSettings } from "../../../Utils/useUserSettings";

interface GroupItemProps {
    group: GroupNode;
    nodesPath: GroupNode[];
    level: number;
    currentBranchName: string | undefined;
    renderGroupList: (groups: GroupNode[], nodesPath: GroupNode[], level: number) => React.JSX.Element;
    currentGroupOrProject: GroupNode | Project;
    rootGroup: GroupNode;
    usePipelineGrouping: boolean;
    allPipelineRuns: PipelineRunsQueryRow[];
    allJobs: JobIdWithParentProject[];
    allJobRuns: JobsQueryRow[];
    noRuns: string | undefined;
}

export function GroupItemDashboardTable({
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
    const [collapsed, setCollapsed] = useUserSettings(
        ["ui", ...nodesPath.map(n => n.id), group.id, "collapsed"],
        false
    );

    return (
        <React.Fragment key={group.id}>
            <thead>
                <tr>
                    <td colSpan={5} style={{ paddingLeft: 25 * level }}>
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
            {!collapsed && (
                <>
                    {(group.projects ?? []).map(x => (
                        <ProjectItemDashboardTable
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
                </>
            )}
        </React.Fragment>
    );
}
