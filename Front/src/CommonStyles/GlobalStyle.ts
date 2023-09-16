import {createGlobalStyle} from "styled-components";

export const GlobalStyle = createGlobalStyle(props => ({
    body: {
        backgroundColor: props.theme.primaryBackground,
        color: props.theme.primaryTextColor,
    }
}));