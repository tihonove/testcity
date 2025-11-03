import { ShapeSquareIcon16Regular, ShareNetworkIcon } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Paging } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchSelect } from "../Components/BranchSelect";
import { NumberCell } from "../Components/Cells";
import { SvgPieChart } from "../Components/PieChart";
import { JobRunNames } from "../Domain/Storage/JobsQuery";
import { useBasePrefix } from "../Domain/Navigation";
import { TestPerJobRunQueryRowNames } from "../Domain/TestPerJobRunQueryRow";
import { formatDuration } from "../Components/RunStatisticsChart/DurationUtils";
import { RunStatisticsChart } from "../Components/RunStatisticsChart/RunStatisticsChart";
import { getOffsetTitle, toLocalTimeFromUtc, useSearchParam, useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { RunStatus } from "../Components/RunStatus";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import styles from "./TestHistoryPage.module.css";

export function TestHistoryPage(): React.JSX.Element {
    const [testId] = useSearchParam("id");
    const basePrefix = useBasePrefix();
    const [_, startTransition, isFading] = useDelayedTransition();
    const { pathToGroup, groupNodes } = useProjectContextFromUrlParams();
    const currentProjectId = groupNodes[groupNodes.length - 1].id;
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);

    const [pageRaw, setPage] = useSearchParamAsState("page");
    const page = React.useMemo(() => (isNaN(Number(pageRaw ?? "0")) ? 0 : Number(pageRaw ?? "0")), [pageRaw]);

    if (testId == null) return <div>Test id not specified</div>;

    const jobs = useStorageQuery(x => x.findAllJobs([currentProjectId]), [currentProjectId]);
    const jobIds = jobs.map(x => x[0]);

    const stats = useStorageQuery(
        x => x.getTestStats(testId, jobIds, currentBranchName),
        [testId, jobIds, currentBranchName]
    );
    const testRuns = useStorageQuery(
        x => x.getTestRuns(testId, jobIds, currentBranchName, page),
        [testId, jobIds, currentBranchName, page]
    );
    const totalRunCount = useStorageQuery(
        x => x.getTestRunCount(testId, jobIds, currentBranchName),
        [testId, jobIds, currentBranchName]
    );

    const buildStatus = (status: string, message?: string): string => {
        if (!message) return status;
        return `${message}. ${status}`;
    };

    const getStatusCellClass = (status: RunStatus) => {
        if (status === "Success") return `${styles.statusCellBase} ${styles.statusCellSuccess}`;
        if (status === "Failed") return `${styles.statusCellBase} ${styles.statusCellFailed}`;
        return styles.statusCellBase;
    };

    const [suiteId, testName] = splitTestId(testId);
    // Success Rate: 0.7% (Last 149 Runs) 148 failed 1 successful Download
    return (
        <ColumnStack gap={4} block stretch>
            <Fit>
                <GroupBreadcrumps branchName={currentBranchName} nodes={groupNodes} />
            </Fit>

            <Fit>
                <h1 className={styles.title}>Test History: {testName}</h1>
                <div className={styles.suiteName}>{suiteId}</div>
            </Fit>
            <Fit>
                <RowStack verticalAlign="center">
                    <Fit>
                        <SvgPieChart
                            percentage={stats.reduce((x, y) => (y[0] == "Success" ? x + 1 : x), 0) / stats.length}
                            size={20}
                        />
                    </Fit>
                    <Fit>
                        <span className={styles.successRateText}>
                            {`Success Rate: ${(
                                (stats.reduce((x, y) => (y[0] == "Success" ? x + 1 : x), 0) / stats.length) *
                                100
                            ).toFixed(1)}% (Last ${stats.length.toString()} Runs)`}
                        </span>
                        <span className={styles.failedText}>
                            {`${stats.reduce((x, y) => (y[0] != "Success" ? x + 1 : x), 0).toString()} failed`}
                        </span>
                        <span className={styles.successfulText}>
                            {`${stats.reduce((x, y) => (y[0] == "Success" ? x + 1 : x), 0).toString()} successful`}
                        </span>
                    </Fit>
                </RowStack>
            </Fit>
            <Fit>
                <RowStack gap={2}>
                    <Fit>
                        <BranchSelect
                            branch={currentBranchName}
                            pathToGroup={pathToGroup}
                            onChangeBranch={setCurrentBranchName}
                        />
                    </Fit>
                </RowStack>
            </Fit>
            <Fit>
                <RunStatisticsChart value={stats} />
            </Fit>
            <Fit>
                <React.Suspense fallback={"Loading test list...."}>
                    <SuspenseFadingWrapper fading={isFading}>
                        <table className={styles.testRunsTable}>
                            <thead>
                                <tr className={styles.testRunsTableHeadRow}>
                                    <th>#</th>
                                    <th>Status</th>
                                    <th>Duration</th>
                                    <th>Branch</th>
                                    <th>Job</th>
                                    <th>Started {getOffsetTitle()}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {testRuns.map(x => (
                                    <tr
                                        className={`${styles.testRunsTableRow} ${styles.testRunsTableRowHover}`}
                                        key={
                                            x[TestPerJobRunQueryRowNames.JobId] +
                                            "-" +
                                            x[TestPerJobRunQueryRowNames.JobRunId]
                                        }>
                                        <NumberCell>
                                            <Link to={x[TestPerJobRunQueryRowNames.JobUrl]} target="_blank">
                                                #{x[TestPerJobRunQueryRowNames.JobRunId]}
                                            </Link>
                                        </NumberCell>
                                        <td className={getStatusCellClass(x[TestPerJobRunQueryRowNames.State])}>
                                            {buildStatus(
                                                x[TestPerJobRunQueryRowNames.State],
                                                x[TestPerJobRunQueryRowNames.CustomStatusMessage]
                                            )}
                                        </td>
                                        <td className={styles.durationCell}>{formatDuration(x[4], x[4])}</td>
                                        <td>
                                            <ShareNetworkIcon /> {x[JobRunNames.BranchName]}
                                        </td>
                                        <td className={styles.jobIdCell}>
                                            <Link to={`${basePrefix}jobs/${x[TestPerJobRunQueryRowNames.JobId]}`}>
                                                <ShapeSquareIcon16Regular /> {x[TestPerJobRunQueryRowNames.JobId]}
                                            </Link>
                                        </td>
                                        <td className={styles.startDateCell}>
                                            {toLocalTimeFromUtc(x[TestPerJobRunQueryRowNames.StartDateTime])}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </SuspenseFadingWrapper>
                </React.Suspense>
                <Paging
                    activePage={page + 1}
                    onPageChange={x => {
                        startTransition(() => {
                            setPage((x - 1).toString());
                        });
                    }}
                    pagesCount={Math.ceil(totalRunCount / 50)}
                />
            </Fit>
        </ColumnStack>
    );
}

function splitTestId(testId: string): [string, string] {
    const testIdParts = testId.match(/(?:[^."]+|"[^"]*")+/g);
    if (testIdParts == null || testIdParts.length < 3) {
        return ["", testId];
    }
    return [testIdParts.slice(0, -2).join("."), testIdParts.slice(-2).join(".")];
}
