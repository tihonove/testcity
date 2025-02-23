export type Severity = "info" | "minor" | "major" | "critical" | "blocker";

export const SEVERITY_VALUES: Severity[] = ["blocker", "critical", "major", "minor", "info"];

export function refineSeverities(keys: (string | undefined)[]): Severity[] {
    const unique = new Set<string>(keys.filter(x => x != null));
    return [...SEVERITY_VALUES.filter(s => unique.has(s))];
}
