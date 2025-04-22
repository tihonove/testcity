import { ShareNetworkIcon, UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";

import { useDeferredValue } from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient, useStorage, useStorageQuery } from "../ClickhouseClientHooksWrapper";
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
import { OverviewTab } from "../Components/CodeQuality/Overview/OverviewTab";
import { IssuesTab } from "../Components/CodeQuality/Issues/IssuesTab";
import { Issue } from "../Components/CodeQuality/types/Issue";
import { BranchBox } from "../Components/BranchBox";
import { theme } from "../Theme/ITheme";
import { format, parseISO } from "date-fns";
import { FailedTestListView } from "../Components/FailedTestListView";
import { Spoiler } from "../Components/Spoiler";
import { CommitRow } from "../Components/CommitRow";

export function JobRunTestListPage(): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const { groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const projectId = groupNodes[groupNodes.length - 1].id;
    const { jobId = "", jobRunId = "" } = useParams();
    useSearchParamDebouncedAsState("filter", 500, "");

    const client = useClickhouseClient();
    const jobInfo =
        useStorageQuery(x => x.getJobInfo(projectId, jobId, jobRunId), [projectId, jobId, jobRunId]) ??
        reject("JobInfo not found");
    const [
        ,
        ,
        branchName,
        agentName,
        startDateTime,
        endDateTime,
        totalTestsCount,
        agentOSName,
        duration,
        successTestsCount,
        skippedTestsCount,
        failedTestsCount,
        state,
        customStatusMessage,
        jobUrl,
        ,
        pipelineSource,
        triggered,
        hasCodeQualityReport,
        coveredCommits,
        totalCoveredCommitCount,
    ] = jobInfo;

    const [section, setSection] = useSearchParamAsState("section", "overview");

    return (
        <Root>
            <ColumnStack gap={4} block stretch>
                <Fit>
                    <GroupBreadcrumps branchName={branchName} nodes={groupNodes} jobId={jobId} />
                </Fit>
                <Fit>
                    <ColorByState state={state}>
                        <JobRunHeader>
                            <JonRunIcon size={32} />
                            <StyledLink to={jobUrl}>#{jobRunId}</StyledLink>&nbsp;at {formatDateTime(startDateTime)}
                        </JobRunHeader>
                        <StatusMessage>{customStatusMessage}</StatusMessage>
                        <TestsStatusMessage>
                            Tests passed: {successTestsCount}
                            {Number(failedTestsCount) > 0 && `, failed: ${failedTestsCount.toString()}`}
                            {Number(skippedTestsCount) > 0 && `, ignored: ${skippedTestsCount.toString()}`}.
                        </TestsStatusMessage>
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

                <Fit>
                    <Tabs value={section ?? "tests"} onValueChange={setSection}>
                        <Tabs.Tab id="overview">Overview</Tabs.Tab>
                        <Tabs.Tab id="tests">Tests</Tabs.Tab>
                        {Boolean(hasCodeQualityReport) && (
                            <Tabs.Tab id="code-quality-overview">Code quality - overview</Tabs.Tab>
                        )}
                        {Boolean(hasCodeQualityReport) && (
                            <Tabs.Tab id="code-quality-issues">Code quality - issues</Tabs.Tab>
                        )}
                    </Tabs>
                </Fit>
                <Fit>
                    {section === "overview" && (
                        <ColumnStack gap={4} stretch block>
                            {failedTestsCount > 0 && (
                                <Fit>
                                    <Spoiler
                                        openedByDefault={true}
                                        iconSize={18}
                                        title={
                                            <>
                                                <OverviewSectionHeader>Failed tests</OverviewSectionHeader> (first 50)
                                            </>
                                        }>
                                        <FailedTestListView
                                            jobRunIds={[jobRunId]}
                                            pathToProject={pathToGroup}
                                            failedTestsCount={Number(failedTestsCount)}
                                            linksBlock={
                                                <Fit>
                                                    <Link
                                                        to={`${basePrefix}jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/treemap`}>
                                                        Open tree map
                                                    </Link>
                                                </Fit>
                                            }
                                        />
                                    </Spoiler>
                                </Fit>
                            )}
                            {totalCoveredCommitCount > 0 && (
                                <Fit>
                                    <Spoiler
                                        iconSize={18}
                                        title={
                                            <OverviewSectionHeader>
                                                {totalCoveredCommitCount} changes
                                            </OverviewSectionHeader>
                                        }
                                        openedByDefault={true}>
                                        <ColumnStack gap={2} stretch block>
                                            {coveredCommits.map(commit => (
                                                <Fit>
                                                    <CommitRow
                                                        sha={Array.isArray(commit) ? commit[0] : commit.CommitSha}
                                                        authorName={
                                                            Array.isArray(commit) ? commit[1] : commit.AuthorName
                                                        }
                                                        authorEmail={
                                                            Array.isArray(commit) ? commit[2] : commit.AuthorEmail
                                                        }
                                                        messagePreview={
                                                            Array.isArray(commit) ? commit[3] : commit.MessagePreview
                                                        }
                                                    />
                                                </Fit>
                                            ))}
                                        </ColumnStack>
                                    </Spoiler>
                                </Fit>
                            )}
                        </ColumnStack>
                    )}
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

function formatDateTime(dateTimeString: string): string {
    if (!dateTimeString) {
        return "";
    }
    try {
        const date = parseISO(dateTimeString);
        if (isNaN(date.getTime())) {
            return dateTimeString;
        }
        return format(date, "d MMM HH:mm");
    } catch (e) {
        return dateTimeString;
    }
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

export interface CommitRowProps {
    sha: string;
    authorName: string;
    authorEmail: string;
    messagePreview: string;
}

const StyledLink = styled(Link)`
    color: inherit;
    font-size: inherit;
    line-height: inherit;
    display: inherit;
`;

const JobRunHeader = styled.h1`
    ${theme.typography.pages.header1};
    display: flex;
`;

const StatusMessage = styled.h3`
    display: flex;
    line-height: 32px;
`;

const TestsStatusMessage = styled.span`
    display: flex;
    line-height: 32px;
`;

const Root = styled.main``;

const OverviewSectionHeader = styled.span`
    font-size: 18px;
    line-height: 24px;
`;
