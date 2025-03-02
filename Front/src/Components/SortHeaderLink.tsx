import {
    UiFilterSortADefaultIcon16Regular,
    UiFilterSortALowToHighIcon16Regular,
    UiFilterSortAHighToLowIcon16Regular,
} from "@skbkontur/icons";
import * as React from "react";
import styled from "styled-components";

type SortHeaderLinkProps = {
    sortKey: string;
    onChangeSortKey: (nextValue: string | undefined) => void;
    currentSortKey: string | undefined;
    currentSortDirection: string | undefined;
    onChangeSortDirection: (nextValue: string | undefined) => void;
    children: React.ReactNode;
};
export function SortHeaderLink(props: SortHeaderLinkProps): React.JSX.Element {
    return (
        <SortHeaderLinkRoot
            href="#"
            onClick={() => {
                if (props.sortKey == props.currentSortKey) {
                    if (props.currentSortDirection == undefined) props.onChangeSortDirection("desc");
                    else if (props.currentSortDirection == "desc") props.onChangeSortDirection("asc");
                    else {
                        props.onChangeSortKey(undefined);
                        props.onChangeSortDirection(undefined);
                    }
                } else {
                    props.onChangeSortKey(props.sortKey);
                    props.onChangeSortDirection(props.currentSortDirection);
                }
                return false;
            }}>
            {props.children}{" "}
            {props.sortKey == props.currentSortKey ? (
                props.currentSortDirection == undefined ? (
                    <UiFilterSortADefaultIcon16Regular />
                ) : props.currentSortDirection == "asc" ? (
                    <UiFilterSortALowToHighIcon16Regular />
                ) : (
                    <UiFilterSortAHighToLowIcon16Regular />
                )
            ) : (
                <UiFilterSortADefaultIcon16Regular />
            )}
        </SortHeaderLinkRoot>
    );
}

const SortHeaderLinkRoot = styled.a`
    font-size: 12px;
    color: ${props => props.theme.mutedTextColor};
    white-space: nowrap;
    text-decoration: none;

    &:hover {
        text-decoration: underline;
    }
`;
