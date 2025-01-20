import * as React from "react";
import { ThemeProvider } from "styled-components";
import { darkTheme, normalTheme } from "./ITheme";
import { DARK_THEME, LIGHT_THEME, ThemeContext } from "@skbkontur/react-ui";
import { useDarkMode } from "usehooks-ts";

interface TestAnalyticsThemeProviderProps {
    children?: React.JSX.Element | React.JSX.Element[];
}

export function TestAnalyticsThemeProvider(props: TestAnalyticsThemeProviderProps): React.JSX.Element {
    const { isDarkMode } = useDarkMode();

    return (
        <ThemeProvider theme={isDarkMode ? darkTheme : normalTheme}>
            <ThemeContext.Provider value={isDarkMode ? DARK_THEME : LIGHT_THEME}>
                {props.children}
            </ThemeContext.Provider>
        </ThemeProvider>
    );
}
