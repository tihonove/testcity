import { Gapped, Loader, Tabs } from "@skbkontur/react-ui";
import React, { useEffect, useState } from "react";
import { OverviewTab } from "../Components/CodeQuality/Overview/OverviewTab";
import { IssuesTab } from "../Components/CodeQuality/Issues/IssuesTab";
import { Issue } from "../Components/CodeQuality/types/Issue";
import { useParams } from "react-router-dom";
import { useApiUrl, useBasePrefix } from "../Domain/Navigation";

export function CodeQualityPage() {
    const apiUrl = useApiUrl();
    const { projectId = "", jobId = "" } = useParams();
    const [loading, setLoading] = useState(false);
    const [report, setReport] = useState<undefined | Issue[]>();

    const fetcher = async () => {
        setLoading(true);
        try {
            const res = await fetch(`${apiUrl}gitlab/${projectId}/jobs/${jobId}/codequality`);
            // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
            setReport(await res.json());
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        fetcher();
    }, [projectId, jobId]);

    const [tab, setTab] = useState<"overview" | "issues">("overview");
    return (
        <Loader type="big" active={loading}>
            <Gapped vertical gap={20}>
                <Tabs value={tab} onValueChange={setTab}>
                    <Tabs.Tab id="overview">Overview</Tabs.Tab>
                    <Tabs.Tab id="issues">Issues</Tabs.Tab>
                </Tabs>
                {tab === "overview" && <OverviewTab current={report} />}
                {tab === "issues" && <IssuesTab report={report ?? []} />}
            </Gapped>
        </Loader>
    );
}
