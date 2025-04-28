import React, { useState } from "react";
import { Gapped, Switcher, SwitcherItems } from "@skbkontur/react-ui";
import { Issue } from "../types/Issue";
import { groupBySeverity } from "../Overview/utils";
import { refineSeverities } from "../types/Severity";
import { SeverityIcon } from "../SeverityIcon";
import { Severity } from "../types/Severity";
import { SeverityOverview } from "../Overview/types";
import { IssuesTable } from "./IssuesTable";
import { IssuesTree } from "./IssuesTree";
import { MultipleSelect } from "./MultipleSelect";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import styles from "./IssuesTab.module.css";

interface IssuesTabProps {
    report: Issue[];
}

export function IssuesTab({ report }: IssuesTabProps) {
    const severities = refineSeverities(report.map(x => x.severity));
    const overview = groupBySeverity(report);

    const [severity, setSeverity] = useState("");
    const [prefix, setPrefix] = useState("");
    const [categories, setCategories] = useState<string[]>([]);
    const [ids, setIds] = useState<string[]>([]);

    const availableCategories = [...new Set<string>(report.map(x => x.categories ?? []).flat())].sort();
    const availableIds = [...new Set<string>(report.map(x => x.check_name).filter(x => x != null))].sort();
    const filtered = report.filter(
        (x: Issue) =>
            (!severity || x.severity === severity) &&
            (!categories.length || (x.categories ?? []).some(c => categories.includes(c))) &&
            (!ids.length || ids.includes(x.check_name ?? ""))
    );

    return (
        <ColumnStack gap={5} block stretch>
            <Fit>
                <RowStack block baseline gap={2}>
                    <Fit>
                        <Switcher
                            items={severityItems(severities, overview)}
                            value={severity}
                            onValueChange={setSeverity}
                            caption="Severity"
                        />
                    </Fit>
                    <Fit>
                        <MultipleSelect
                            caption="Categories"
                            items={availableCategories}
                            selected={categories}
                            onChange={setCategories}
                        />
                    </Fit>
                    <Fit>
                        <MultipleSelect caption="IDs" items={availableIds} selected={ids} onChange={setIds} />
                    </Fit>
                </RowStack>
            </Fit>
            <Fit>
                <RowStack block verticalAlign="top" gap={5}>
                    <Fit className={styles.issuesTreeFit}>
                        <IssuesTree issues={filtered} prefix={prefix} onChangePrefix={setPrefix} />
                    </Fit>
                    <Fill>
                        <IssuesTable
                            key={severity}
                            report={prefix ? filtered.filter(x => x.location.path.startsWith(prefix)) : filtered}
                        />
                    </Fill>
                </RowStack>
            </Fit>
        </ColumnStack>
    );
}

function severityItems(
    severities: Severity[],
    overview: Partial<Record<Severity | "unknown", SeverityOverview>>
): SwitcherItems[] {
    const all = Object.values(overview)
        .map(x => x.total)
        .reduce((a, b) => a + b, 0);
    return [
        {
            label: `All ${all.toString()}`,
            value: "",
            buttonProps: {
                title: `All issues: ${all.toString()}`,
            },
        },
        ...severities.map(s => ({
            value: s,
            label: overview[s]?.total.toString() ?? "",
            buttonProps: {
                title: `${overview[s]?.total.toString() ?? ""} ${s}`,
                icon: <SeverityIcon type={s} />,
            },
        })),
    ];
}
