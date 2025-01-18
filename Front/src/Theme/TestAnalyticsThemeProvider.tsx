import * as React from "react";
import { ThemeProvider } from "styled-components";
import { normalTheme } from "./ITheme";
import { LIGHT_THEME, ThemeContext } from "@skbkontur/react-ui";

interface TestAnalyticsThemeProviderProps {
    children?: React.JSX.Element | React.JSX.Element[];
}

export function TestAnalyticsThemeProvider(props: TestAnalyticsThemeProviderProps): React.JSX.Element {
    return (
        <ThemeProvider theme={normalTheme}>
            <ThemeContext.Provider value={LIGHT_THEME}>{props.children}</ThemeContext.Provider>
        </ThemeProvider>
    );
}
