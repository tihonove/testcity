import * as React from "react";
import { useDarkMode } from "./UseDarkModeFixed";

export const ForcedDarkModeContext = React.createContext<boolean | undefined>(undefined);

export function useTestAnalyticsDarkMode(): { isDarkMode: boolean; toggle: () => void } {
    const forcedValue = React.useContext(ForcedDarkModeContext);
    const { isDarkMode, toggle } = useDarkMode({ localStorageKey: "test-analytics-dark-mode" });
    return { isDarkMode: forcedValue ?? isDarkMode, toggle };
}
