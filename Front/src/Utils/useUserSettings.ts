import { useLocalStorage } from "usehooks-ts";

export function useUserSettings<T>(path: string[], defaultValue: T): [T, (value: T) => void] {
    const [settings, setSettings] = useLocalStorage<Record<string, unknown>>("userSettings", {});

    const getValue = (obj: Record<string, unknown>, keys: string[]): T => {
        let current: unknown = obj;
        for (const key of keys) {
            if (current == null || typeof current !== "object" || !(key in current)) {
                return defaultValue;
            }
            current = (current as Record<string, unknown>)[key];
        }
        return current as T;
    };

    const setValue = (newValue: T) => {
        setSettings(prevSettings => {
            const newSettings = { ...prevSettings };
            let current: Record<string, unknown> = newSettings;

            for (let i = 0; i < path.length - 1; i++) {
                const key = path[i];
                if (!current[key] || typeof current[key] !== "object") {
                    current[key] = {};
                }
                current = current[key] as Record<string, unknown>;
            }

            current[path[path.length - 1]] = newValue;
            return newSettings;
        });
    };

    const currentValue = getValue(settings, path);
    return [currentValue, setValue];
}
