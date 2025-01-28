import { Link } from "react-router-dom";
import styled, { css } from "styled-components";

export const BranchCell = styled.td<{ branch: string }>`
    max-width: 200px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;

    ${props =>
        props.branch == "master" &&
        css`
            border-radius: 20px;
            background: ${props => props.theme.accentBgColor};
            color: ${props => props.theme.accentTextColor};
            display: inline;
            padding: 2px 13px 2px 8px !important;
        `}
`;

export const ColorByState = styled.span<{ state: string }>`
    color: ${props =>
        props.state == "Success"
            ? props.theme.successTextColor
            : props.state == "Canceled"
              ? props.theme.mutedTextColor
              : props.theme.failedTextColor};
`;

export const JobLinkWithResults = styled(Link)<{ state: string }>`
    color: ${props =>
        props.state == "Success"
            ? props.theme.successTextColor
            : props.state == "Canceled"
              ? props.theme.mutedTextColor
              : props.theme.failedTextColor};
    text-decoration: none;
    &:hover {
        text-decoration: underline;
    }
`;

export const NumberCell = styled.td`
    width: 80px;
`;

export const SelectedOnHoverTr = styled.tr`
    &:hover {
        background-color: ${props => props.theme.backgroundColor1};
    }
`