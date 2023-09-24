import type {Preview} from "@storybook/react";
import "../src/CommonStyles/Fonts.css";
import "../src/CommonStyles/Reset.css";
import "../src/CommonStyles/BaseTypography.css";
import {THEME_2022_DARK, ThemeContext} from "@skbkontur/react-ui";
import * as React from "react";
import {ThemeProvider} from "styled-components";
import {darkTheme} from "../src/Theme/ITheme";
import {GlobalStyle} from "../src/CommonStyles/GlobalStyle";
import {TestAnalyticsThemeProvider} from "../src/Theme/TestAnalyticsThemeProvider";


const preview: Preview = {
    decorators: [
        (Story) => (
            <TestAnalyticsThemeProvider>
                <GlobalStyle/>
                <Story/>
            </TestAnalyticsThemeProvider>
        ),
    ],
    parameters: {
        actions: {argTypesRegex: "^on[A-Z].*"},
        controls: {
            matchers: {
                color: /(background|color)$/i,
                date: /Date$/,
            },
        },
    },
};

export default preview;
