import { UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import * as React from "react";
import styled from "styled-components";
import { theme } from "../Theme/ITheme";

export function KebabButton() {
    return (
        <KebabButtonRoot>
            <UiMenuDots3VIcon16Regular />
        </KebabButtonRoot>
    );
}

export const KebabButtonRoot = styled.span`
    display: inline-block;
    padding: 0 2px 1px 2px;
    border-radius: 10px;
    cursor: pointer;

    &:hover {
        background-color: ${theme.backgroundColor1};
    }
`;
