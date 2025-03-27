import { Link } from "react-router-dom";
import { css, styled } from "styled-components";
import { theme } from "../Theme/ITheme";

export const BranchCell = styled.td`
    max-width: 200px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
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
        background-color: ${theme.inverseColor("0.1")};
    }
`;
