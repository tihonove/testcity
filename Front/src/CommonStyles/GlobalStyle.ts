import { createGlobalStyle } from "styled-components";

export const GlobalStyle = createGlobalStyle`
    a {
      color: ${props => props.theme.primaryTextColor};

      &:hover {
        color: ${props => props.theme.activeLinkColor};
      }
        
        &.no-underline {
            text-decoration: none;
            
            &:hover {
                text-decoration: underline;
            }
        }
    }
    
    body {
        background-color: ${props => props.theme.primaryBackground};
        color: ${props => props.theme.primaryTextColor};
    }
`;
