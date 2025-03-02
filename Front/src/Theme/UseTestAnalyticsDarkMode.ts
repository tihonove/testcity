import * as React from "react";
import { useDarkMode } from "./UseDarkModeFixed";
import { useBasePrefix } from "../Domain/Navigation";

export const ForcedDarkModeContext = React.createContext<boolean | undefined>(undefined);

export function useTestAnalyticsDarkMode(): { isDarkMode: boolean; toggle: () => void } {
    const basePrefix = useBasePrefix();
    const forcedValue = React.useContext(ForcedDarkModeContext);
    const { isDarkMode, toggle } = useDarkMode({ localStorageKey: basePrefix + "-dark-mode" });
    return { isDarkMode: forcedValue ?? isDarkMode, toggle };
}
