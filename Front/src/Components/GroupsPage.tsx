import * as React from "react";
import styled from "styled-components";
import { theme } from "../Theme/ITheme";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Input } from "@skbkontur/react-ui";
import { Group, useRootGroups } from "../Domain/Storage";
import { Link } from "react-router-dom";
import { FoldersIcon16Regular, FoldersIcon20Light, FoldersIcon24Regular } from "@skbkontur/icons";

function groupLink(groupIdOrTitleList: string[]): string {
    return `/test-analytics/${groupIdOrTitleList.map(x => encodeURIComponent(x)).join("/")}`;
}

export function GroupsPage() {
    const [searchText, setSearchText] = React.useState("");
    const projects = useRootGroups();
    const filteredGroups = React.useMemo(
        () => projects.filter(p => !searchText.trim() || p.title.toLowerCase().includes(searchText.toLowerCase())),
        [searchText, projects]
    );

    return (
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
                                    <FoldersIcon20Light />
                                </Fit>
                                <Fit>
                                    <GroupAvatar group={p}></GroupAvatar>
                                </Fit>
                                <Fit>
                                    <RootGroupTitle to={groupLink([p.title])}>{p.title}</RootGroupTitle>
                                </Fit>
                            </RowStack>
                        </Fit>
                    ))}
                </ColumnStack>
            </Content>
        </Root>
    );
}

interface GroupAvatarProps {
    group: Group;
}

export function GroupAvatar(props: GroupAvatarProps) {
    return (
        <GroupAvatarRoot style={{ backgroundColor: deterministicColor(props.group.title) }}>
            {props.group.title[0]}
        </GroupAvatarRoot>
    );
}

function deterministicColor(input: string) {
    let hash = 0;
    for (let i = 0; i < input.length; i++) {
        hash = input.charCodeAt(i) + ((hash << 5) - hash);
    }
    const color = (hash & 0x00ffffff).toString(16).toUpperCase();
    return `#${"000000".substring(0, 6 - color.length) + color}20`;
}

const GroupAvatarRoot = styled.div`
    font-size: 20px;
    line-height: 32px;
    text-align: center;
    text-transform: uppercase;
    width: 32px;
    height: 32px;
    border-radius: 4px;
    outline: 1px solid ${theme.borderLineColor2};
    outline-offset: -1px;
`;

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
