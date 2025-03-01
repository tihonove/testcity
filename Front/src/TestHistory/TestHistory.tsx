import * as React from "react";
import styled from "styled-components";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Paging } from "@skbkontur/react-ui";
import { ShapeSquareIcon16Regular, ShareNetworkIcon } from "@skbkontur/icons";
import { RunStatisticsChart } from "../RunStatisticsChart/RunStatisticsChart";
import { formatDuration } from "../RunStatisticsChart/DurationUtils";
import { Link } from "react-router-dom";
import { BranchSelect } from "./BranchSelect";
import { getOffsetTitle, toLocalTimeFromUtc } from "../Utils";
import { ProjectComboBox } from "../Components/ProjectComboBox";
import { ArrowARightIcon, HomeIcon, JonIcon } from "../Components/Icons";
import { Suspense } from "react";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { JobRunNames } from "../Domain/JobsQueryRow";

export type RunStatus = "Failed" | "Skipped" | "Success";

interface TestHistoryProps {
    testId: string;
    jobId: undefined | string;
    onChangeJobId: (value: undefined | string) => void;
    jobIds: string[];

    branch: undefined | string;
    onChangeBranch: (value: undefined | string) => void;
    branchNames: string[];

    stats: Array<[state: string, duration: number, startDate: string]>;

    totalRunCount: number;
    runsPage: number;
    onRunsPageChange: (nextPage: number) => void;
    runsFetcher: () => Array<
        [
            jobId: string,
            jobRunId: string,
            branchName: string,
            state: RunStatus,
            duration: number,
            startDateTime: string,
            agentName: string,
            agentOSName: string,
            jobUrl: string,
        ]
    >;
    statusMessages: Array<[jobRunId: string, customStatusMessage: string]>;
    runIdBreadcrumb: string | undefined;
}

function splitTestId(testId: string): [string, string] {
    const testIdParts = testId.match(/(?:[^."]+|"[^"]*")+/g);
    if (testIdParts == null || testIdParts.length < 3) {
        return ["", testId];
    }
    return [testIdParts.slice(0, -2).join("."), testIdParts.slice(-2).join(".")];
}

export function TestHistory(props: TestHistoryProps): React.JSX.Element {
    const [suiteId, testId] = splitTestId(props.testId);
    const basePrefix = useBasePrefix();

    const getStatusMessage = (jobRunId: string): string => {
        return props.statusMessages.find(m => m[0] === jobRunId)?.[1] ?? "";
    };

    const buildStatus = (status: string, message?: string): string => {
        if (!message) return status;
        return `${message}. ${status}`;
    };

    return (
        <ColumnStack gap={4} block stretch>
            <Fit>
                <HomeHeader>
                    <Link to={`/${basePrefix}/jobs`}>
                        <HomeIcon size={16} /> All jobs
                    </Link>
                    <ArrowARightIcon size={16} />
                    <Link to={`/${basePrefix}/jobs/${props.jobIds[0]}`}>
                        <JonIcon size={16} /> {props.jobIds[0]}
                    </Link>
                    {props.runIdBreadcrumb && (
                        <>
                            <ArrowARightIcon size={16} />
                            <Link to={`/${basePrefix}/jobs/${props.jobIds[0]}/runs/${props.runIdBreadcrumb}`}>
                                <JonIcon size={16} /> {props.runIdBreadcrumb}
                            </Link>
                        </>
                    )}
                </HomeHeader>
            </Fit>
            <Fit>
                <Title>Test History: {testId}</Title>
                <SuiteName>{suiteId}</SuiteName>
            </Fit>
            <Fit>
                {props.stats.reduce((x, y) => (y[0] == "Success" ? x + 1 : x), 0)} Success |{" "}
                {props.stats.reduce((x, y) => (y[0] != "Success" ? x + 1 : x), 0)} Failed (of {props.stats.length}{" "}
                total)
            </Fit>
            <Fit>
                <RowStack gap={2}>
                    <Fit>
                        <ProjectComboBox value={props.jobId} items={props.jobIds} handler={props.onChangeJobId} />
                    </Fit>
                    <Fit>
                        <BranchSelect
                            branch={props.branch}
                            branchNames={props.branchNames}
                            onChangeBranch={props.onChangeBranch}
                        />
                    </Fit>
                </RowStack>
            </Fit>
            <Fit>
                <RunStatisticsChart value={props.stats} />
            </Fit>
            <Fit>
                <Suspense fallback={"Loading test list...."}>
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
                            {props.runsFetcher().map(x => (
                                <TestRunsTableRow key={x[0] + "-" + x[1]}>
                                    <NumberCell>
                                        <Link to={x[6]} target="_blank">
                                            #{x[1]}
                                        </Link>
                                    </NumberCell>
                                    <StatusCell status={x[3]}>{buildStatus(x[3], getStatusMessage(x[1]))}</StatusCell>
                                    <DurationCell>{formatDuration(x[4], x[4])}</DurationCell>
                                    <BranchCell $defaultBranch={x[JobRunNames.BranchName] == "master"}>
                                        <ShareNetworkIcon /> {x[JobRunNames.BranchName]}
                                    </BranchCell>
                                    <JobIdCell>
                                        <Link to={`/${basePrefix}/jobs/${x[0]}`}>
                                            <ShapeSquareIcon16Regular /> {x[0]}
                                        </Link>
                                    </JobIdCell>
                                    <StartDateCell>{toLocalTimeFromUtc(x[5])}</StartDateCell>
                                </TestRunsTableRow>
                            ))}
                        </tbody>
                    </TestRunsTable>
                </Suspense>
                <Paging
                    activePage={props.runsPage}
                    onPageChange={props.onRunsPageChange}
                    pagesCount={Math.ceil(props.totalRunCount / 50)}
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
