import * as React from "react";
import styled from "styled-components";
import { theme } from "../Theme/ITheme";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { Input } from "@skbkontur/react-ui";
import { useRootGroups } from "../Domain/Storage";
import { Link } from "react-router-dom";

function groupLink(groupIdOrTitleList: string[]): string {
    return `/test-analytics/${groupIdOrTitleList.map(x => encodeURIComponent(x)).join("/")}`;
}

export function ProjectsPage() {
    const [searchText, setSearchText] = React.useState("");
    const projects = useRootGroups();
    const filteredGroups = React.useMemo(
        () => projects.filter(p => !searchText.trim() || p.title.toLowerCase().includes(searchText.toLowerCase())),
        [searchText, projects]
    );

    return (
        <Root>
            <Content>
                <ColumnStack gap={1} block horizontalAlign="stretch">
                    <Fit>
                        <Header>Projects</Header>
                    </Fit>
                    <Fit>
                        <Input value={searchText} onValueChange={setSearchText} size="large" width="1000px" />
                    </Fit>
                    {filteredGroups.map(p => (
                        <Fit key={p.id}>
                            <RootGroupTitle to={groupLink([p.title])}>{p.title}</RootGroupTitle>
                        </Fit>
                    ))}
                </ColumnStack>
            </Content>
        </Root>
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
