import { UiMenuShapeCircle4Icon20Light } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Input } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { GroupAvatar } from "../Components/GroupAvatar";
import { groupLink, useBasePrefix } from "../Domain/Navigation";
import { LogoPageBlock } from "../Components/LogoPageBlock";
import { useRootGroups } from "../Components/useProjectContextFromUrlParams";
import styles from "./GroupsPage.module.css";

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
            <main className={styles.root}>
                <div className={styles.content}>
                    <ColumnStack gap={2} block horizontalAlign="stretch">
                        <Fit>
                            <h1>Groups</h1>
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
                                        <Link className={styles.rootGroupTitle} to={groupLink(basePrefix, [p.title])}>
                                            {p.title}
                                        </Link>
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
                                    <div className={styles.addProjectAvatarRoot20}>+</div>
                                </Fit>
                                <Fit>
                                    <Link className={styles.addGroupTitleButton} to={"add-project"}>
                                        Add new...
                                    </Link>
                                </Fit>
                            </RowStack>
                        </Fit>
                    </ColumnStack>
                </div>
            </main>
        </>
    );
}
