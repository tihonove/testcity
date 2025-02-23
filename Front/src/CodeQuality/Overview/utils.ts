import { Issue } from "../types/Issue";
import { Severity } from "../types/Severity";
import { DiffOverview, SeverityOverview } from "./types";

export function groupBySeverity(issues: Issue[]): Partial<Record<Severity | "unknown", SeverityOverview>> {
    return issues.reduce((acc: Partial<Record<Severity | "unknown", SeverityOverview>>, issue) => {
        const severity = issue.severity;
        if (acc[severity] == undefined) {
            acc[severity] = {
                total: 0,
                issues: {},
            };
        }

        acc[severity].total++;

        const type = issue.check_name ?? "unknown";
        // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
        if (!acc[severity].issues[type]) {
            acc[severity].issues[type] = {
                count: 0,
                content: issue.content,
                categories: issue.categories,
            };
        }

        acc[severity].issues[type].count++;

        return acc;
    }, {});
}

export function diff(baseline: Issue[], current: Issue[]): DiffOverview {
    const baselineFingerprints = new Set<string>(baseline.map(x => x.fingerprint));
    const currentFingerprints = new Set<string>(current.map(x => x.fingerprint));
    const diff = currentFingerprints.symmetricDifference(baselineFingerprints);

    const fixed = [...diff].map(x => baseline.find(y => y.fingerprint === x)).filter(x => x != null);

    const added = [...diff].map(x => current.find(y => y.fingerprint === x)).filter(x => x != null);

    return {
        fixed: groupBySeverity(fixed),
        added: groupBySeverity(added),
    };
}
