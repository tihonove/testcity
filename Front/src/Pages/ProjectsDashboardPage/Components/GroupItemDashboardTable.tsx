import {
    ArrowShapeTriangleADownIcon20Light,
    ArrowShapeTriangleARightIcon20Light,
    UiMenuShapeCircle4Icon20Light,
} from "@skbkontur/icons";
import { Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link } from "react-router-dom";
import { GroupAvatar } from "../../../Components/GroupAvatar";
import { addBranchToLink } from "../../../Domain/Navigation";
import { ProjectItemDashboardTable } from "./ProjectItemDashboardTable";

import { GroupDashboardNode } from "../../../Domain/ProjectDashboardNode";
import { useUserSettings } from "../../../Utils/useUserSettings";
import styles from "./GroupItemDashboardTable.module.css";

interface GroupItemProps {
    group: GroupDashboardNode;
    level: number;
    currentBranchName: string | undefined;
    noRuns: string | undefined;
    doNotRenderHeader?: boolean;
}

export function GroupItemDashboardTable({
    group,
    level,
    currentBranchName,
    noRuns,
    doNotRenderHeader,
}: GroupItemProps): React.JSX.Element {
    const [collapsed, setCollapsed] = useUserSettings(["ui", ...group.fullPathSlug.map(n => n.id), "collapsed"], false);

    return (
        <React.Fragment key={group.id}>
            {!doNotRenderHeader && (
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
                                        <Link to={addBranchToLink(group.link, currentBranchName)}>
                                            <h3 className={styles.header3}>{group.title}</h3>
                                        </Link>
                                    </Fit>
                                </RowStack>
                            </div>
                        </td>
                    </tr>
                </thead>
            )}
            {(!collapsed || doNotRenderHeader) && (
                <>
                    {group.children
                        .filter(x => x.type === "project")
                        .map(x => (
                            <ProjectItemDashboardTable
                                key={x.id}
                                project={x}
                                level={level + 1}
                                currentBranchName={currentBranchName}
                                noRuns={noRuns}
                            />
                        ))}
                    {group.children
                        .filter(x => x.type === "group")
                        .map(x => (
                            <GroupItemDashboardTable
                                key={x.id}
                                group={x}
                                level={level + 1}
                                currentBranchName={currentBranchName}
                                noRuns={noRuns}
                            />
                        ))}
                </>
            )}
        </React.Fragment>
    );
}
