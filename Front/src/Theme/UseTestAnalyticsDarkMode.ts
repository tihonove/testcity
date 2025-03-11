import * as React from "react";
import { useTernaryDarkMode } from "usehooks-ts";

export const ForcedDarkModeContext = React.createContext<boolean | undefined>(undefined);

export function useTestAnalyticsDarkMode(): {
    isDarkMode: boolean;
    ternaryDarkMode: "system" | "dark" | "light";
    toggle: () => void;
} {
    const forcedValue = React.useContext(ForcedDarkModeContext);
    const { isDarkMode, ternaryDarkMode, toggleTernaryDarkMode } = useTernaryDarkMode({
        localStorageKey: "test-analytics-dark-mode-ternary",
    });
    return { isDarkMode: forcedValue ?? isDarkMode, ternaryDarkMode, toggle: toggleTernaryDarkMode };
}
