import * as React from "react";
import styled from "styled-components";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Paging } from "@skbkontur/react-ui";
import {
    ArrowUiCornerOutUpRightIcon,
    LogoMicrosoftIcon,
    QuestionCircleIcon, ShapeCircleMIcon16Regular,
    ShapeSquareIcon16Regular,
    ShareNetworkIcon
} from "@skbkontur/icons";
import { RunStatisticsChart } from "../RunStatisticsChart/RunStatisticsChart";
import { formatDuration } from "../RunStatisticsChart/DurationUtils";
import { Link } from "react-router-dom";
import { BranchSelect } from "./BranchSelect";
import {getOffsetTitle, toLocalTimeFromUtc} from "../Utils";
import {JobComboBox} from "../Components/JobComboBox";
import {ArrowARightIcon, HomeIcon, JonIcon} from "../Components/Icons";

export type RunStatus = 'Failed' | 'Skipped' | 'Success';

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
    runs: Array<
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

    return (
        <ColumnStack gap={4} block stretch>
            <Fit>
                <HomeHeader>
                    <Link to={`/test-analytics/jobs`}>
                        <HomeIcon size={16} /> All jobs  
                    </Link>
                    <ArrowARightIcon size={16} />
                    <Link to={`/test-analytics/jobs/${props.jobIds[0]}`}>
                        <JonIcon size={16} />  {props.jobIds[0]}  
                    </Link>
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
                        <JobComboBox value={props.jobId} items={props.jobIds} handler={props.onChangeJobId}/>
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
                <TestRunsTable>
                    <thead>
                        <TestRunsTableHeadRow>
                            <th>Status</th>
                            <th>Duration</th>
                            <th>Branch</th>
                            <th>Build</th>
                            <th>Agent</th>
                            <th>Started {getOffsetTitle()}</th>
                            <th>Job Url</th>
                        </TestRunsTableHeadRow>
                    </thead>
                    <tbody>
                        {props.runs.map(x => (
                            <TestRunsTableRow key={x[0] + "-" + x[1]}>
                                <StatusCell status={x[3]}>{x[3]}</StatusCell>
                                <DurationCell>{formatDuration(x[4], x[4])}</DurationCell>
                                <BranchCell>
                                    <ShareNetworkIcon /> {x[2]}
                                </BranchCell>
                                <td>
                                    <Link to={`/test-analytics/jobs/${x[0]}`}>
                                        <ShapeSquareIcon16Regular /> {x[0]}
                                    </Link>
                                    {" / "}
                                    <Link to={`/test-analytics/jobs/${x[0]}/runs/${x[1]}`}>
                                        <ShapeCircleMIcon16Regular /> #{x[1]}
                                    </Link>
                                </td>
                                <td>
                                    {x[7] == "Windows" ? <LogoMicrosoftIcon /> : <QuestionCircleIcon />} {x[6]}
                                </td>
                                <StartDateCell>{toLocalTimeFromUtc(x[5])}</StartDateCell>
                                {x[8] && <GitLabLinkCell><Link to={x[8]} target="_blank" ><ArrowUiCornerOutUpRightIcon/></Link></GitLabLinkCell>}
                            </TestRunsTableRow>
                        ))}
                    </tbody>
                </TestRunsTable>
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

const StatusCell = styled.td<{ status: RunStatus }>`
    width: 100px;
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

const GitLabLinkCell = styled.td`
    text-align: center !important;
`;

const BranchCell = styled.td``;

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

const TestRunsTableRow = styled.tr({
    td: {
        textAlign: "left",
        padding: "6px 8px",
    },
});

const TestRunsTable = styled.table({
    width: "100%",
});
