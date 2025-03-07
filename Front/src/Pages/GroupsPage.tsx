import { FoldersIcon20Light, UiMenuShapeCircle4Icon20Light } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Input } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useRootGroups } from "../Domain/Storage";
import { groupLink, useBasePrefix } from "../Domain/Navigation";
import { theme } from "../Theme/ITheme";
import { GroupAvatar } from "../Components/GroupAvatar";
import { Logo } from "../Components/Logo";
import { LogoPageBlock } from "./LogoPageBlock";

export function GroupsPage() {
    const [searchText, setSearchText] = React.useState("");
    const projects = useRootGroups();
    const filteredGroups = React.useMemo(
        () => projects.filter(p => !searchText.trim() || p.title.toLowerCase().includes(searchText.toLowerCase())),
        [searchText, projects]
    );
    const basePrefix = useBasePrefix();

    return (
        <>
            <LogoPageBlock />
            <Root>
                <Content>
                    <ColumnStack gap={2} block horizontalAlign="stretch">
                        <Fit>
                            <Header>Groups</Header>
                        </Fit>
                        <Fit>
                            <Input
                                placeholder="Search"
                                value={searchText}
                                onValueChange={setSearchText}
                                size="large"
                                width="1000px"
                            />
                        </Fit>
                        {filteredGroups.map(p => (
                            <Fit key={p.id}>
                                <RowStack gap={2} block verticalAlign="center">
                                    <Fit>
                                        <UiMenuShapeCircle4Icon20Light />
                                    </Fit>
                                    <Fit>
                                        <GroupAvatar size="20px" group={p}></GroupAvatar>
                                    </Fit>
                                    <Fit>
                                        <RootGroupTitle to={groupLink(basePrefix, [p.title])}>{p.title}</RootGroupTitle>
                                    </Fit>
                                </RowStack>
                            </Fit>
                        ))}
                    </ColumnStack>
                </Content>
            </Root>
        </>
    );
}

const Root = styled.main`
    padding-top: 100px;
`;

const Content = styled.div`
    margin: 0 auto;
    max-width: ${theme.layout.centered.width};
`;

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;

const RootGroupTitle = styled(Link)`
    display: block;
    font-size: 20px;
    line-height: 40px;
    text-decoration: none;

    :hover {
        background-color: ${theme.backgroundColor1};
    }
`;

