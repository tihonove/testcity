import { BuildingHomeIcon16Light, UiMenuShapeCircle4Icon16Light } from "@skbkontur/icons";
import { RowStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { useBasePrefix, groupLink, createLinkToJob } from "../Domain/Navigation";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { Link } from "react-router-dom";
import styles from "./GroupBreadcrumps.module.css";

interface GroupBreadcrumpsProps {
    nodes: (GroupNode | Project)[];
    branchName: undefined | string;
    jobId?: undefined | string;
}

export function GroupBreadcrumps(props: GroupBreadcrumpsProps) {
    const basePrefix = useBasePrefix();

    return (
        <RowStack gap={2} block>
            <Fit>
                <Link className={styles.rootGroupTitleLink} to={basePrefix}>
                    <BuildingHomeIcon16Light />
                    {props.nodes.length == 0 ? " All groups" : ""}
                </Link>
            </Fit>
            {props.nodes.map((p, index) => (
                <React.Fragment key={p.id}>
                    {<Fit> / </Fit>}
                    <Fit>
                        <RowStack gap={2} block verticalAlign="center">
                            <Fit>
                                <UiMenuShapeCircle4Icon16Light />
                            </Fit>
                            <Fit>
                                <Link
                                    to={groupLink(
                                        basePrefix,
                                        props.nodes.slice(0, index + 1).map(x => x.title),
                                        props.branchName
                                    )}>
                                    {p.title}
                                </Link>
                            </Fit>
                        </RowStack>
                    </Fit>
                </React.Fragment>
            ))}
            {props.jobId && (
                <React.Fragment key={props.jobId}>
                    {<Fit> / </Fit>}
                    <Fit>
                        <RowStack gap={2} block verticalAlign="center">
                            <Fit>
                                <UiMenuShapeCircle4Icon16Light />
                            </Fit>
                            <Fit>
                                <Link
                                    to={createLinkToJob(
                                        props.nodes[0],
                                        props.nodes[props.nodes.length - 1].id,
                                        props.jobId,
                                        props.branchName
                                    )}>
                                    {props.jobId}
                                </Link>
                            </Fit>
                        </RowStack>
                    </Fit>
                </React.Fragment>
            )}
        </RowStack>
    );
}
