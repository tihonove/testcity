import { useDarkMode } from "./UseDarkModeFixed";

export function useTestAnalyticsDarkMode(): { isDarkMode: boolean; toggle: () => void } {
    const { isDarkMode, toggle } = useDarkMode({ localStorageKey: "test-analytics-dark-mode" });
    return { isDarkMode, toggle };
}
