import styled from "styled-components";
import { theme } from "../Theme/ITheme";

export const NativeLinkButton = styled.span`
    cursor: pointer;
    color: ${theme.activeLinkColor};
    text-decoration: none;

    &:hover {
        text-decoration: underline;
    }
`;
