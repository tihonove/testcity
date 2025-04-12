import { UiMenuShapeCircle4Icon20Light } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Input } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { GroupAvatar } from "./GroupAvatar";
import { groupLink, useBasePrefix } from "../Domain/Navigation";
import { useRootGroups } from "../Domain/Storage/Storage";
import { theme } from "../Theme/ITheme";
import { LogoPageBlock } from "./LogoPageBlock";
import { useSearchParamAsState } from "../Utils";
import { AddNewProjectModal } from "./AddNewProjectModal";

export function GroupsPage() {
    const [searchText, setSearchText] = React.useState("");
    const [showAddNewProjectModal, setShowAddNewProjectModal] = useSearchParamAsState("how-to-add");
    const projects = useRootGroups();
    const filteredGroups = React.useMemo(
        () => projects.filter(p => !searchText.trim() || p.title.toLowerCase().includes(searchText.toLowerCase())),
        [searchText, projects]
    );
    const basePrefix = useBasePrefix();

    const handleAddNewProject = () => {
        setShowAddNewProjectModal("1");
    };

    return (
        <>
            {showAddNewProjectModal === "1" && (
                <AddNewProjectModal
                    onClose={() => {
                        setShowAddNewProjectModal(undefined);
                    }}
                />
            )}
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
                        <Fit key="new-item">
                            <RowStack gap={2} block verticalAlign="center">
                                <Fit>
                                    <UiMenuShapeCircle4Icon20Light />
                                </Fit>
                                <Fit>
                                    <AddProjectAvatarRoot20>+</AddProjectAvatarRoot20>
                                </Fit>
                                <Fit>
                                    <AddGroupTitleButton onClick={handleAddNewProject}>Add new...</AddGroupTitleButton>
                                </Fit>
                            </RowStack>
                        </Fit>
                    </ColumnStack>
                </Content>
            </Root>
        </>
    );
}

const AddProjectAvatarRoot20 = styled.div`
    font-size: 20px;
    line-height: 32px;
    text-align: center;
    text-transform: uppercase;
    width: 32px;
    height: 32px;
    border-radius: 4px;
    outline: 2px dashed ${theme.borderLineColor2};
    outline-offset: -2px;
    background: ${theme.backgroundColor2};
`;

const Root = styled.main`
    padding-top: 100px;
`;

const Content = styled.div`
    margin: 0 auto;
    max-width: ${theme.layout.centered.width};
`;

const Header = styled.h1`
    ${theme.typography.pages.header1};
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

const AddGroupTitleButton = styled.button`
    border: 0;
    background: none;
    display: block;
    font-size: 20px;
    line-height: 40px;
    color: ${theme.primaryTextColor};
    font-family: inherit;
    cursor: pointer;
    padding: 0;
    text-decoration: none;
`;
