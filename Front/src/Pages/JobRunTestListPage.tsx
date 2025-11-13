import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";

import { Loader, Tabs } from "@skbkontur/react-ui";
import { format, parseISO } from "date-fns";
import { Link, useParams } from "react-router-dom";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { BranchBox } from "../Components/BranchBox";
import { ColorByState } from "../Components/Cells";
import { IssuesTab } from "../Components/CodeQuality/Issues/IssuesTab";
import { OverviewTab } from "../Components/CodeQuality/Overview/OverviewTab";
import { Issue } from "../Components/CodeQuality/types/Issue";
import { CommitRow } from "../Components/CommitRow";
import { FailedTestListView } from "../Components/FailedTestListView";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { JonRunIcon } from "../Components/Icons";
import { Spoiler } from "../Components/Spoiler";
import { TestListView } from "../Components/TestListView";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import {
    createLinkToProject,
    createLinkToProjectByPath,
    createLinkToTreeMap,
    useApiUrl,
    useBasePrefix,
} from "../Domain/Navigation";
import { useSearchParamAsState, useSearchParamDebouncedAsState } from "../Utils";
import styles from "./JobRunTestListPage.module.css";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";

export function JobRunTestListPage(): React.JSX.Element {
    const { rootGroup, pathToGroup } = useProjectContextFromUrlParams();
    const projectId = rootGroup.id;
    const { jobId = "", jobRunId = "" } = useParams();
    useSearchParamDebouncedAsState("filter", 500, "");

    const jobInfo = useTestCityRequest(
        x => x.runs.getJobRun(pathToGroup, jobId, jobRunId),
        [pathToGroup, jobId, jobRunId]
    );
    const {
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
        pipelineSource,
        triggered,
        hasCodeQualityReport,
        changesSinceLastRun: coveredCommits,
        totalCoveredCommitCount,
    } = jobInfo;

    const [section, setSection] = useSearchParamAsState("section", "overview");

    return (
        <main className={styles.root}>
            <ColumnStack gap={4} block stretch>
                <Fit>
                    <GroupBreadcrumps branchName={branchName} pathToProject={pathToGroup} jobId={jobId} />
                </Fit>
                <Fit>
                    <ColorByState state={state}>
                        <h1 className={styles.jobRunHeader}>
                            <JonRunIcon size={32} />
                            <Link className={styles.styledLink} to={jobUrl}>
                                #{jobRunId}
                            </Link>
                            &nbsp;at {formatDateTime(startDateTime)}
                        </h1>
                        <h3 className={styles.statusMessage}>{customStatusMessage}</h3>
                        <span className={styles.testsStatusMessage}>
                            Tests passed: {successTestsCount}
                            {Number(failedTestsCount) > 0 && `, failed: ${Number(failedTestsCount).toString()}`}
                            {Number(skippedTestsCount) > 0 && `, ignored: ${Number(skippedTestsCount).toString()}`}.
                        </span>
                    </ColorByState>
                </Fit>
                <Fit>
                    <BranchBox name={branchName} />
                </Fit>
                <Fit>
                    <AdditionalJobInfo
                        startDateTime={startDateTime}
                        endDateTime={endDateTime}
                        duration={duration ?? 0}
                        triggered={triggered ?? ""}
                        pipelineSource={pipelineSource ?? ""}
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
                            {Number(failedTestsCount) > 0 && (
                                <Fit>
                                    <Spoiler
                                        openedByDefault={true}
                                        iconSize={18}
                                        title={
                                            <>
                                                <span className={styles.overviewSectionHeader}>Failed tests</span>{" "}
                                                (first 50)
                                            </>
                                        }>
                                        <FailedTestListView
                                            projectId={projectId}
                                            jobId={jobId}
                                            jobRunId={jobRunId}
                                            pathToProject={pathToGroup}
                                            failedTestsCount={Number(failedTestsCount)}
                                            linksBlock={
                                                <Fit>
                                                    <Link
                                                        to={createLinkToTreeMap(
                                                            createLinkToProjectByPath(pathToGroup),
                                                            jobId,
                                                            jobRunId
                                                        )}>
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
                                            <span className={styles.overviewSectionHeader}>
                                                {totalCoveredCommitCount} changes
                                            </span>
                                        }
                                        openedByDefault={true}>
                                        <ColumnStack gap={2} stretch block>
                                            {coveredCommits.map(commit => (
                                                <Fit key={commit.parentCommitSha}>
                                                    <CommitRow
                                                        pathToProject={pathToGroup}
                                                        sha={commit.parentCommitSha}
                                                        authorName={commit.authorName}
                                                        authorEmail={commit.authorEmail}
                                                        messagePreview={commit.messagePreview}
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
                            projectId={projectId}
                            jobId={jobId}
                            jobRunId={jobRunId}
                            pathToProject={pathToGroup}
                            linksBlock={
                                <Fit>
                                    <Link
                                        to={createLinkToTreeMap(
                                            createLinkToProjectByPath(pathToGroup),
                                            jobId,
                                            jobRunId
                                        )}>
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
        </main>
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
