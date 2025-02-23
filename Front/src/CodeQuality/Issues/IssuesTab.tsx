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
        <div>
            <Gapped gap={24}>
                <Switcher
                    items={severityItems(severities, overview)}
                    value={severity}
                    onValueChange={setSeverity}
                    caption="Severity"
                />
                <MultipleSelect
                    caption="Categories"
                    items={availableCategories}
                    selected={categories}
                    onChange={setCategories}
                />
                <MultipleSelect caption="IDs" items={availableIds} selected={ids} onChange={setIds} />
            </Gapped>
            <div style={{ paddingTop: 24, gridTemplateColumns: "1fr 2fr", display: "grid", gap: "1.5rem" }}>
                <IssuesTree issues={filtered} prefix={prefix} onChangePrefix={setPrefix} />
                <IssuesTable
                    key={severity}
                    report={prefix ? filtered.filter(x => x.location.path.startsWith(prefix)) : filtered}
                />
            </div>
        </div>
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
