import React from "react";
import { Issue } from "../types/Issue";
import { OverviewDiff } from "./OverviewDiff";
import { OverviewTotal } from "./OverviewTotal";

interface OverviewTabProps {
    baseline?: Issue[];
    current?: Issue[];
}

export function OverviewTab({ baseline, current }: OverviewTabProps) {
    return !current ? (
        <Empty />
    ) : baseline ? (
        <OverviewDiff baseline={baseline} current={current} />
    ) : (
        <OverviewTotal report={current} />
    );
}

export function Empty() {
    return <div>Empty!</div>;
}
