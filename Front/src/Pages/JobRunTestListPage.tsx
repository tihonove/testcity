import { ShareNetworkIcon, UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";

import { useDeferredValue } from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { ColorByState } from "../Components/Cells";
import { JonRunIcon } from "../Components/Icons";
import { useApiUrl, useBasePrefix } from "../Domain/Navigation";
import { reject } from "../Utils/TypeHelpers";
import { useSearchParam, useSearchParamAsState, useSearchParamDebouncedAsState } from "../Utils";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { TestListView } from "../Components/TestListView";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { Loader, Tabs } from "@skbkontur/react-ui";
import { OverviewTab } from "../CodeQuality/Overview/OverviewTab";
import { IssuesTab } from "../CodeQuality/Issues/IssuesTab";
import { Issue } from "../CodeQuality/types/Issue";
import { BranchBox } from "../Components/BranchBox";

export function JobRunTestListPage(): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const { groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const projectId = groupNodes[groupNodes.length - 1].id;
    const { jobId = "", jobRunId = "" } = useParams();
    useSearchParamDebouncedAsState("filter", 500, "");

    const client = useClickhouseClient();

    const data = client.useData2<
        [string, string, number, string, string, string, string, string, string, string, string, string, string, number]
    >(
        `
        SELECT 
            StartDateTime, 
            EndDateTime, 
            Duration, 
            BranchName, 
            TotalTestsCount, 
            SuccessTestsCount, 
            FailedTestsCount, 
            SkippedTestsCount, 
            Triggered, 
            PipelineSource, 
            JobUrl, 
            CustomStatusMessage, 
            State,
            HasCodeQualityReport
        FROM JobInfo 
        WHERE 
            JobId = '${jobId}' 
            AND JobRunId in ['${jobRunId}']
        `,
        [jobId, jobRunId]
    );

    const [
        startDateTime,
        endDateTime,
        duration,
        branchName,
        totalTestsCount,
        successTestsCount,
        failedTestsCount,
        skippedTestsCount,
        triggered,
        pipelineSource,
        jobUrl,
        customStatusMessage,
        state,
        hasCodeQualityReport,
    ] = data[0] ?? reject("JobRun not found");
    const [section, setSection] = useSearchParamAsState("section", "tests");

    return (
        <Root>
            <ColumnStack gap={4} block stretch>
                <Fit>
                    <GroupBreadcrumps branchName={branchName} nodes={groupNodes} />
                </Fit>
                <Fit>
                    <ColorByState state={state}>
                        <JobRunHeader>
                            <JonRunIcon size={32} />
                            <StyledLink to={jobUrl}>#{jobRunId}</StyledLink>&nbsp;at {startDateTime}
                        </JobRunHeader>
                        <StatusMessage>{customStatusMessage}</StatusMessage>
                    </ColorByState>
                </Fit>
                <Fit>
                    <BranchBox name={branchName} />
                </Fit>
                <Fit>
                    <AdditionalJobInfo
                        startDateTime={startDateTime}
                        endDateTime={endDateTime}
                        duration={duration}
                        triggered={triggered}
                        pipelineSource={pipelineSource}
                    />
                </Fit>
                {hasCodeQualityReport && (
                    <Fit>
                        <Tabs value={section ?? "tests"} onValueChange={setSection}>
                            <Tabs.Tab id="tests">Tests</Tabs.Tab>
                            <Tabs.Tab id="code-quality-overview">Code quality - overview</Tabs.Tab>
                            <Tabs.Tab id="code-quality-issues">Code quality - issues</Tabs.Tab>
                        </Tabs>
                    </Fit>
                )}
                <Fit>
                    {section === "tests" && (
                        <TestListView
                            jobRunIds={[jobRunId]}
                            pathToProject={pathToGroup}
                            successTestsCount={Number(successTestsCount)}
                            failedTestsCount={Number(failedTestsCount)}
                            skippedTestsCount={Number(skippedTestsCount)}
                            linksBlock={
                                <Fit>
                                    <Link
                                        to={`${basePrefix}jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/treemap`}>
                                        Open tree map
                                    </Link>
                                </Fit>
                            }
                        />
                    )}
                    {section === "code-quality-overview" && (
                        <CodeQualityOverviewTabContent jobId={jobRunId} projectId={projectId} />
                    )}
                    {section === "code-quality-issues" && (
                        <CodeQualityIssuesTabContent jobId={jobRunId} projectId={projectId} />
                    )}
                </Fit>
            </ColumnStack>
        </Root>
    );
}

function CodeQualityOverviewTabContent({ projectId, jobId }: { projectId: string; jobId: string }) {
    const apiUrl = useApiUrl();
    const [loading, setLoading] = React.useState(false);
    const [report, setReport] = React.useState<undefined | Issue[]>();

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

    React.useEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        fetcher();
    }, [projectId, jobId]);

    return (
        <Loader type="big" active={loading}>
            {report && <OverviewTab current={report} />}
        </Loader>
    );
}

function CodeQualityIssuesTabContent({ projectId, jobId }: { projectId: string; jobId: string }) {
    const apiUrl = useApiUrl();
    const [loading, setLoading] = React.useState(false);
    const [report, setReport] = React.useState<undefined | Issue[]>();

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

    React.useEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        fetcher();
    }, [projectId, jobId]);

    return (
        <Loader type="big" active={loading}>
            {report && <IssuesTab report={report} />}
        </Loader>
    );
}

const StyledLink = styled(Link)`
    color: inherit;
    font-size: inherit;
    line-height: inherit;
    display: inherit;
`;

const JobRunHeader = styled.h1`
    display: flex;
    font-size: 32px;
    line-height: 32px;
`;

const StatusMessage = styled.h3`
    display: flex;
    line-height: 32px;
`;

const Root = styled.main``;
