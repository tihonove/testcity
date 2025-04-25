import { createGlobalStyle } from "styled-components";

export const GlobalStyle = createGlobalStyle`
    a {
        color: ${props => props.theme.primaryTextColor};
        text-decoration: none;

        &:hover {
            color: ${props => props.theme.activeLinkColor};
            text-decoration: underline;
        }
    }
    
    body {
        background-color: ${props => props.theme.primaryBackground};
        color: ${props => props.theme.primaryTextColor};
    }

    .lucide {
        display: inline-block;
        vertical-align: middle;
    }
`;
