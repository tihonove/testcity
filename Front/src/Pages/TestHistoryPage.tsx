import { ShapeSquareIcon16Regular, ShareNetworkIcon } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Paging } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { BranchSelect } from "../Components/BranchSelect";
import { NumberCell } from "../Components/Cells";
import { SvgPieChart } from "../Components/PieChart";
import { useBasePrefix } from "../Domain/Navigation";
import { formatDuration } from "../Components/RunStatisticsChart/DurationUtils";
import { RunStatisticsChart } from "../Components/RunStatisticsChart/RunStatisticsChart";
import { getOffsetTitle, toLocalTimeFromUtc, useSearchParam, useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { RunStatus } from "../Components/RunStatus";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import styles from "./TestHistoryPage.module.css";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";

export function TestHistoryPage(): React.JSX.Element {
    const [testId] = useSearchParam("id");
    const basePrefix = useBasePrefix();
    const [_, startTransition, isFading] = useDelayedTransition();
    const { pathToGroup } = useProjectContextFromUrlParams();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);

    const [pageRaw, setPage] = useSearchParamAsState("page");
    const page = React.useMemo(() => (isNaN(Number(pageRaw ?? "0")) ? 0 : Number(pageRaw ?? "0")), [pageRaw]);

    if (testId == null) return <div>Test id not specified</div>;

    const stats = useTestCityRequest(
        x => x.runs.getTestStats(pathToGroup, testId, currentBranchName),
        [testId, pathToGroup, currentBranchName]
    );

    const testRuns = useTestCityRequest(
        x => x.runs.getTestRuns(pathToGroup, testId, currentBranchName, page),
        [testId, pathToGroup, currentBranchName, page]
    );
    const totalRunCount = useTestCityRequest(
        x => x.runs.getTestRunCount(pathToGroup, testId, currentBranchName),
        [testId, pathToGroup, currentBranchName]
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
    const statsTuples = React.useMemo(() => stats.map(x => [x.state, x.duration, x.startDateTime] as const), [stats]);
    // Success Rate: 0.7% (Last 149 Runs) 148 failed 1 successful Download
    return (
        <ColumnStack gap={4} block stretch>
            <Fit>
                <GroupBreadcrumps branchName={currentBranchName} pathToProject={pathToGroup} />
            </Fit>

            <Fit>
                <h1 className={styles.title}>Test History: {testName}</h1>
                <div className={styles.suiteName}>{suiteId}</div>
            </Fit>
            <Fit>
                <RowStack verticalAlign="center">
                    <Fit>
                        <SvgPieChart
                            percentage={stats.reduce((x, y) => (y.state == "Success" ? x + 1 : x), 0) / stats.length}
                            size={20}
                        />
                    </Fit>
                    <Fit>
                        <span className={styles.successRateText}>
                            {`Success Rate: ${(
                                (stats.reduce((x, y) => (y.state == "Success" ? x + 1 : x), 0) / stats.length) *
                                100
                            ).toFixed(1)}% (Last ${stats.length.toString()} Runs)`}
                        </span>
                        <span className={styles.failedText}>
                            {`${stats.reduce((x, y) => (y.state != "Success" ? x + 1 : x), 0).toString()} failed`}
                        </span>
                        <span className={styles.successfulText}>
                            {`${stats.reduce((x, y) => (y.state == "Success" ? x + 1 : x), 0).toString()} successful`}
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
                <RunStatisticsChart value={statsTuples} />
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
                                        key={x.jobId + "-" + x.jobRunId}>
                                        <NumberCell>
                                            <Link to={x.jobUrl} target="_blank">
                                                #{x.jobRunId}
                                            </Link>
                                        </NumberCell>
                                        <td className={getStatusCellClass(x.state)}>
                                            {buildStatus(x.state, x.customStatusMessage)}
                                        </td>
                                        <td className={styles.durationCell}>
                                            {formatDuration(x.duration, x.duration)}
                                        </td>
                                        <td>
                                            <ShareNetworkIcon /> {x.branchName}
                                        </td>
                                        <td className={styles.jobIdCell}>
                                            <Link to={`${basePrefix}jobs/${x.jobId}`}>
                                                <ShapeSquareIcon16Regular /> {x.jobId}
                                            </Link>
                                        </td>
                                        <td className={styles.startDateCell}>{toLocalTimeFromUtc(x.startDateTime)}</td>
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
