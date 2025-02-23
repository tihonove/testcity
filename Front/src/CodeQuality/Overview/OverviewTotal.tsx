import React, { useState } from "react";
import { SeverityIcon } from "../SeverityIcon";
import { ArrowCRightIcon16Regular } from "@skbkontur/icons/ArrowCRightIcon16Regular";
import { ArrowCDownIcon16Regular } from "@skbkontur/icons/ArrowCDownIcon16Regular";
import { Issue } from "../types/Issue";
import { Severity } from "../types/Severity";
import { SEVERITY_VALUES } from "../types/Severity";

interface OverviewTotalProps {
    report: Issue[];
}

interface IssueType {
    count: number;
    severity?: Severity;
    content?: string;
}

export function OverviewTotal({ report }: OverviewTotalProps) {
    const issuesByType = report.reduce<Record<string, IssueType>>((acc, issue) => {
        const checkName = issue.check_name ?? "";
        acc[checkName] ??= {
            count: 0,
            severity: issue.severity,
            content: issue.content?.body,
        };
        acc[checkName].count++;
        return acc;
    }, {});

    const sortedIssues = Object.entries(issuesByType).sort(([, a], [, b]) => {
        if (a.severity === b.severity) {
            return b.count - a.count;
        }
        return SEVERITY_VALUES.indexOf(a.severity as Severity) - SEVERITY_VALUES.indexOf(b.severity as Severity);
    });

    return (
        <div>
            <h3>Total Issues: {report.length}</h3>
            {sortedIssues.map(([type, issue]) => (
                <IssueOverview issue={issue} type={type} key={type} />
            ))}
        </div>
    );
}

function IssueOverview({ type, issue }: { type: string; issue: IssueType }) {
    const [open, setOpen] = useState(false);

    return (
        <div key={type}>
            {issue.content &&
                (open ? (
                    <ArrowCDownIcon16Regular
                        onClick={() => {
                            setOpen(false);
                        }}
                    />
                ) : (
                    <ArrowCRightIcon16Regular
                        onClick={() => {
                            setOpen(true);
                        }}
                    />
                ))}
            <SeverityIcon type={issue.severity as Severity} /> {type}: {issue.count} issues
            {open && issue.content && <p style={{ paddingLeft: 40 }}>{issue.content}</p>}
        </div>
    );
}
