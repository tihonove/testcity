import * as React from "react";
import { Link, useParams } from "react-router-dom";
import {
    formatTestCounts,
    formatTestDuration,
    getLinkToJob,
    getOffsetTitle,
    toLocalTimeFromUtc,
    useSearchParamAsState
} from "../Utils";
import { BranchSelect } from "../TestHistory/BranchSelect";
import {
    LogoMicrosoftIcon,
    QuestionCircleIcon,
    ShapeSquareIcon32Regular,
    ShareNetworkIcon
} from "@skbkontur/icons";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import styled from "styled-components";
import {HomeIcon} from "../Components/Icons";

export function JobRunsPage(): React.JSX.Element {
    const { jobId } = useParams();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    const client = useClickhouseClient();

    const jobRuns = client.useData2<[string, string, string, string, string, string, string, string, string, string, string, string, string]>(
        `
        SELECT
            JobId,
            JobRunId,
            BranchName,
            AgentName,
            StartDateTime,
            TotalTestsCount,
            SuccessTestsCount,
            SkippedTestsCount,
            FailedTestsCount,
            AgentOSName,
            Duration
        FROM JobInfo
        WHERE JobId = '${jobId}' ${currentBranchName ? `AND BranchName = '${currentBranchName}'` : ""}
        ORDER BY StartDateTime DESC
        LIMIT 100
        `,
        [currentBranchName, jobId]
    );
    
    return (
        <ColumnStack block stretch gap={2}>
            <Fit>
                <HomeHeader>
                    <Link to={`/test-analytics/jobs`}>
                        <HomeIcon size={16} /> All jobs
                    </Link>
                </HomeHeader>
            </Fit>
            <Fit>
                <Header1>
                    <ShapeSquareIcon32Regular /> {jobId}
                </Header1>
            </Fit>
            <Fit>
                <BranchSelect
                    branch={currentBranchName}
                    onChangeBranch={setCurrentBranchName}
                    branchQuery={`SELECT DISTINCT BranchName
                                  FROM JobInfo
                                  WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND BranchName != ''
                                  ORDER BY StartDateTime DESC;`}
                />
            </Fit>
            <Fit>
                <RunList>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>branch</th>
                            <th></th>
                            <th>agent</th>
                            <th>started {getOffsetTitle()}</th>
                            <th>duration</th>
                        </tr>
                    </thead>
                    <tbody>
                        {jobRuns.map(x => (
                            <tr>
                                <NumberCell>
                                    <Link to={getLinkToJob(x[1], x[3])}>#{x[1]}</Link>
                                </NumberCell>
                                <BranchCell>
                                    <ShareNetworkIcon/> {x[2]}
                                </BranchCell>
                                <CountCell>
                                    <JobLinkWithResults failedCount={x[8]} to={`/test-analytics/jobs/${jobId}/runs/${x[1]}`}>
                                        {formatTestCounts(x[5], x[6], x[7], x[8])}
                                    </JobLinkWithResults>
                                </CountCell>
                                <AgentCell>
                                    {/windows/.test(x[9]) ? <LogoMicrosoftIcon /> : <QuestionCircleIcon />} {x[3]}
                                </AgentCell>
                                <StartedCell>{toLocalTimeFromUtc(x[4])}</StartedCell>
                                <DurationCell>{formatTestDuration(x[10])}</DurationCell>
                            </tr>
                        ))}
                    </tbody>
                </RunList>
            </Fit>
        </ColumnStack>
    );
}

const HomeHeader = styled.h2``;

const Header1 = styled.h1`
    font-size: 32px;
    line-height: 40px;
    display: flex;
`;

const RunList = styled.table`
    width: 100%;
    max-width: 100vw;

    th {
        font-size: 12px;
        text-align: left;
        padding: 4px 8px;
    }

    td {
        text-align: left;
        padding: 6px 8px;
    }

    thead > tr {
        border-bottom: 1px solid #eee;
    }
`;

const NumberCell = styled.td`
    width: 80px;
`;

const BranchCell = styled.td``;

const CountCell = styled.td``;

const JobLinkWithResults = styled(Link)<{ failedCount: string }>`
    color: ${props =>
            props.failedCount == "0"
                    ? props.theme.successTextColor
                    : props.theme.failedTextColor};
    text-decoration: none;
    &:hover {
        text-decoration: underline;
    }
`;

const AgentCell = styled.td``;

const StartedCell = styled.td``;

const DurationCell = styled.td``;
