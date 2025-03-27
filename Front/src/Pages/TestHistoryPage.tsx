import { ShapeSquareIcon16Regular, ShareNetworkIcon } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Paging } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchSelect } from "../Components/BranchSelect";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { SvgPieChart } from "../Components/PieChart";
import { JobRunNames } from "../Domain/Storage/JobsQuery";
import { useBasePrefix } from "../Domain/Navigation";
import { TestPerJobRunQueryRowNames } from "../Domain/TestPerJobRunQueryRow";
import { formatDuration } from "../Components/RunStatisticsChart/DurationUtils";
import { RunStatisticsChart } from "../Components/RunStatisticsChart/RunStatisticsChart";
import { theme } from "../Theme/ITheme";
import { getOffsetTitle, toLocalTimeFromUtc, useSearchParam, useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { RunStatus } from "../Components/RunStatus";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";

export function TestHistoryPage(): React.JSX.Element {
    const [testId] = useSearchParam("id");
    const basePrefix = useBasePrefix();
    const [_, startTransition, isFading] = useDelayedTransition();
    const { groupNodes } = useProjectContextFromUrlParams();
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

    const [suiteId, testName] = splitTestId(testId);
    // Success Rate: 0.7% (Last 149 Runs) 148 failed 1 successful Download
    return (
        <ColumnStack gap={4} block stretch>
            <Fit>
                <GroupBreadcrumps branchName={currentBranchName} nodes={groupNodes} />
            </Fit>

            <Fit>
                <Title>Test History: {testName}</Title>
                <SuiteName>{suiteId}</SuiteName>
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
                        <SuccessRateText>
                            {`Success Rate: ${(
                                (stats.reduce((x, y) => (y[0] == "Success" ? x + 1 : x), 0) / stats.length) *
                                100
                            ).toFixed(1)}% (Last ${stats.length.toString()} Runs)`}
                        </SuccessRateText>
                        <FailedText>
                            {`${stats.reduce((x, y) => (y[0] != "Success" ? x + 1 : x), 0).toString()} failed`}
                        </FailedText>
                        <SuccessfulText>
                            {`${stats.reduce((x, y) => (y[0] == "Success" ? x + 1 : x), 0).toString()} successful`}
                        </SuccessfulText>
                    </Fit>
                </RowStack>
            </Fit>
            <Fit>
                <RowStack gap={2}>
                    <Fit>
                        <BranchSelect
                            branch={currentBranchName}
                            projectIds={[currentProjectId]}
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
                    <SuspenseFadingWrapper $fading={isFading}>
                        <TestRunsTable>
                            <thead>
                                <TestRunsTableHeadRow>
                                    <th>#</th>
                                    <th>Status</th>
                                    <th>Duration</th>
                                    <th>Branch</th>
                                    <th>Job</th>
                                    <th>Started {getOffsetTitle()}</th>
                                </TestRunsTableHeadRow>
                            </thead>
                            <tbody>
                                {testRuns.map(x => (
                                    <TestRunsTableRow
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
                                        <StatusCell status={x[TestPerJobRunQueryRowNames.State]}>
                                            {buildStatus(
                                                x[TestPerJobRunQueryRowNames.State],
                                                x[TestPerJobRunQueryRowNames.CustomStatusMessage]
                                            )}
                                        </StatusCell>
                                        <DurationCell>{formatDuration(x[4], x[4])}</DurationCell>
                                        <BranchCell>
                                            <ShareNetworkIcon /> {x[JobRunNames.BranchName]}
                                        </BranchCell>
                                        <JobIdCell>
                                            <Link to={`${basePrefix}jobs/${x[TestPerJobRunQueryRowNames.JobId]}`}>
                                                <ShapeSquareIcon16Regular /> {x[TestPerJobRunQueryRowNames.JobId]}
                                            </Link>
                                        </JobIdCell>
                                        <StartDateCell>
                                            {toLocalTimeFromUtc(x[TestPerJobRunQueryRowNames.StartDateTime])}
                                        </StartDateCell>
                                    </TestRunsTableRow>
                                ))}
                            </tbody>
                        </TestRunsTable>
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

const HomeHeader = styled.h2``;

const StatusCell = styled.td<{ status: RunStatus; width?: string }>`
    max-width: 100px;
    color: ${props =>
        props.status == "Success"
            ? props.theme.successTextColor
            : props.status == "Failed"
              ? props.theme.failedTextColor
              : undefined};
`;

const DurationCell = styled.td`
    width: 100px;
`;

const JobIdCell = styled.td`
    max-width: 200px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const StartDateCell = styled.td`
    width: 200px;
`;

const SuiteName = styled.div`
    font-size: ${props => props.theme.smallTextSize};
    color: ${props => props.theme.mutedTextColor};
`;

const Title = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;

const TestRunsTableHeadRow = styled.tr({
    th: {
        fontSize: "12px",
        textAlign: "left",
        padding: "4px 8px",
    },

    borderBottom: "1px solid #eee",
});

const TestRunsTableRow = styled(SelectedOnHoverTr)({
    td: {
        textAlign: "left",
        padding: "6px 8px",
    },
});

const TestRunsTable = styled.table({
    width: "100%",
});

const SuccessRateText = styled.span`
    font-weight: bold;
`;

const FailedText = styled.span`
    margin-left: 10px;
    color: ${theme.failedTextColor};
    font-size: 14px;
`;

const SuccessfulText = styled.span`
    margin-left: 10px;
    color: ${theme.successTextColor};
    font-size: 14px;
`;

function splitTestId(testId: string): [string, string] {
    const testIdParts = testId.match(/(?:[^."]+|"[^"]*")+/g);
    if (testIdParts == null || testIdParts.length < 3) {
        return ["", testId];
    }
    return [testIdParts.slice(0, -2).join("."), testIdParts.slice(-2).join(".")];
}
