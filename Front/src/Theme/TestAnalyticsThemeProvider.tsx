import * as React from "react";
import { DARK_THEME, LIGHT_THEME, ThemeContext } from "@skbkontur/react-ui";
import { useTestAnalyticsDarkMode } from "./UseTestAnalyticsDarkMode";

interface TestAnalyticsThemeProviderProps {
    children?: React.JSX.Element | React.JSX.Element[];
}

export function TestAnalyticsThemeProvider(props: TestAnalyticsThemeProviderProps): React.JSX.Element {
    const { isDarkMode } = useTestAnalyticsDarkMode();

    React.useEffect(() => {
        const root = document.documentElement;
        if (isDarkMode) {
            root.setAttribute("data-theme", "dark");
        } else {
            root.setAttribute("data-theme", "light");
        }
    }, [isDarkMode]);

    return (
        <ThemeContext.Provider value={isDarkMode ? DARK_THEME : LIGHT_THEME}>{props.children}</ThemeContext.Provider>
    );
}
