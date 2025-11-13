import { BuildingHomeIcon16Light, UiMenuShapeCircle4Icon16Light } from "@skbkontur/icons";
import { Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link } from "react-router-dom";
import { createLinkToJob2, createLinkToProjectByPath, groupLink, useBasePrefix } from "../Domain/Navigation";
import styles from "./GroupBreadcrumps.module.css";

interface GroupBreadcrumpsProps {
    pathToProject: string[];
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
                    {props.pathToProject.length == 0 ? " All groups" : ""}
                </Link>
            </Fit>
            {props.pathToProject.map((p, index) => (
                <React.Fragment key={p}>
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
                                        props.pathToProject.slice(0, index + 1),
                                        props.branchName
                                    )}>
                                    {p}
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
                                    to={createLinkToJob2(
                                        createLinkToProjectByPath(props.pathToProject),
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
