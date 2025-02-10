import { Issue } from '../types/Issue';
import { Severity } from '../types/Severity';
import { DiffOverview, SeverityOverview } from './types';

export const SEVERITY_VALUES: Severity[] = ['blocker', 'critical', 'major', 'minor', 'info'];

export function getPossibleSeverities(keys: string[]): string[] {
  const unique = new Set<string>(keys);
  return [
    ...SEVERITY_VALUES.filter((s) => unique.has(s)),
    ...[...unique].filter((s) => !SEVERITY_VALUES.includes(s as Severity)),
  ];
}

export function groupBySeverity(issues: Issue[]): Record<Severity, SeverityOverview> {
  return issues.reduce(
    (acc, issue) => {
      const severity = issue.severity ?? 'unknown';
      if (!acc[severity]) {
        acc[severity] = {
          total: 0,
          issues: {},
        };
      }

      acc[severity].total++;

      const type = issue.check_name ?? 'unknown';
      if (!acc[severity].issues[type]) {
        acc[severity].issues[type] = {
          count: 0,
          content: issue.content,
          categories: issue.categories,
        };
      }

      acc[severity].issues[type].count++;

      return acc;
    },
    {} as Record<Severity, SeverityOverview>,
  );
}

export function diff(baseline: Issue[], current: Issue[]): DiffOverview {
  const baselineFingerprints = new Set<string>(baseline.map((x) => x.fingerprint));
  const currentFingerprints = new Set<string>(current.map((x) => x.fingerprint));
  const diff = currentFingerprints.symmetricDifference(baselineFingerprints);

  const fixed = [...diff]
    .map((x) => baseline.find((y) => y.fingerprint === x))
    .filter((x) => x != null);

  const added = [...diff]
    .map((x) => current.find((y) => y.fingerprint === x))
    .filter((x) => x != null);

  return {
    fixed: groupBySeverity(fixed),
    added: groupBySeverity(added),
  };
}
