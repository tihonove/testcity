import { ShareNetworkIcon, ShareNetworkIcon16Light } from "@skbkontur/icons";
import * as React from "react";
import { css, styled } from "styled-components";
import { theme } from "../Theme/ITheme";
import { DEFAULT_BRANCHE_NAMES } from "./BranchSelect";

export function BranchBox({ name }: { name: string }) {
    return (
        <Root $defaultBranch={DEFAULT_BRANCHE_NAMES.includes(name)} title={name}>
            <IconWrapper>
                <ShareNetworkIcon16Light />
            </IconWrapper>
            <BranchName>{name}</BranchName>
        </Root>
    );
}

const Root = styled.span<{ $defaultBranch: boolean }>`
    border-radius: 4px;
    border: 1px solid ${theme.borderLineColor2};
    padding: 2px 4px;
    font-size: 14px;
    display: inline-flex;
    align-items: center;
    max-width: 100%;
    overflow: hidden;
    box-sizing: border-box;

    ${props =>
        props.$defaultBranch &&
        css`
            background-color: ${theme.accentBgColor};
            color: ${theme.accentTextColor};
        `}
`;

const IconWrapper = styled.span`
    flex-shrink: 0;
    margin-right: 4px;
`;

const BranchName = styled.span`
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    min-width: 0;
`;
