import { BuildingHomeIcon16Light, UiMenuShapeCircle4Icon16Light } from "@skbkontur/icons";
import { RowStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { useBasePrefix, groupLink } from "../Domain/Navigation";
import { GroupNode, Project } from "../Domain/Storage";
import styled from "styled-components";
import { Link } from "react-router-dom";
import { theme } from "../Theme/ITheme";

interface GroupBreadcrumpsProps {
    nodes: (GroupNode | Project)[];
}

export function GroupBreadcrumps(props: GroupBreadcrumpsProps) {
    const basePrefix = useBasePrefix();

    return (
        <RowStack gap={2} block>
            <Fit>
                <RootGroupTitleLink to={"/" + basePrefix}>
                    <BuildingHomeIcon16Light />
                    {props.nodes.length == 0 ? " All groups" : ""}
                </RootGroupTitleLink>
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
                                <RootGroupTitleLink
                                    to={groupLink(
                                        basePrefix,
                                        props.nodes.slice(0, index + 1).map(x => x.title)
                                    )}>
                                    {p.title}
                                </RootGroupTitleLink>
                            </Fit>
                        </RowStack>
                    </Fit>
                </React.Fragment>
            ))}
        </RowStack>
    );
}

const RootGroupTitleLink = styled(Link)`
    display: block;
    font-size: 16px;
    line-height: 20px;
    text-decoration: none;

    :hover {
        background-color: ${theme.backgroundColor1};
    }
`;
