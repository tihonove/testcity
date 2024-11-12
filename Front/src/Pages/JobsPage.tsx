import {ColumnStack, Fit, RowStack} from "@skbkontur/react-stack-layout";
import * as React from "react";
import styled, { css } from "styled-components";
import {
    ShapeSquareIcon16Regular,
    ShareNetworkIcon,
} from "@skbkontur/icons";
import {useClickhouseClient} from "../ClickhouseClientHooksWrapper";
import {BranchSelect} from "../TestHistory/BranchSelect";
import {formatTestCounts, formatTestDuration, getLinkToJob, getText, toLocalTimeFromUtc, useSearchParamAsState} from "../Utils";
import {Link} from "react-router-dom";
import {JobComboBox} from "../Components/JobComboBox";
import {BranchCell, JobLinkWithResults} from "../Components/BranchCell";

export function JobsPage(): React.JSX.Element {
    const client = useClickhouseClient();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    const [currentGroup, setCurrentGroup] = useSearchParamAsState("group");

    const allGroup = ["Wolfs", "Forms mastering", "Utilities"]
    const allJobs = client
        .useData2<[string]>(`SELECT DISTINCT JobId
                             FROM JobInfo
                             WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`)
        .filter(x => x[0] != null && x[0].trim() !== "");
    
    const allJobRuns = client.useData2<[string, string, string, string, string, string, string, string, string, string, string, string, string]>(
        `
            SELECT
                JobId,
                JobRunId,
                BranchName,
                AgentName,
                StartDateTime,
                TotalTestsCount,
                AgentOSName,
                Duration,
                SuccessTestsCount,
                SkippedTestsCount,
                FailedTestsCount,
                State,
                CustomStatusMessage
            FROM (
                     SELECT
                         *,
                         ROW_NUMBER() OVER (PARTITION BY JobId, BranchName ORDER BY StartDateTime DESC) AS rn
                     FROM JobInfo
                     WHERE StartDateTime >= now() - INTERVAL 3 DAY ${currentBranchName ? `AND BranchName = '${currentBranchName}'` : ""}
                     ) AS filtered
            WHERE rn = 1
            ORDER BY JobId, StartDateTime DESC;`,
        ["1", currentBranchName, currentGroup]
    );

    return (
        <Root>
            <ColumnStack block stretch gap={2}>
                <Fit>
                    <Header>Jobs</Header>
                </Fit>
                <Fit>
                    <RowStack block baseline gap={2}>
                        <Fit>
                            <JobComboBox value={currentGroup} items={allGroup} handler={setCurrentGroup}/>
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
                    </RowStack>
                </Fit>
                {allGroup
                    .filter(s => !currentGroup || s === currentGroup)
                    .map(section => (
                    <>
                        <Fit>
                            <Header3>{section}</Header3>
                        </Fit>
                        <JobList>
                            {allJobs.filter(x => {
                                if (section === "Wolfs")
                                    return !x[0].includes("FM · ") && !x[0].includes("Run ");
                                else if (section === "Forms mastering")
                                    return x[0].includes("FM · ");
                                else if (section === "Utilities")
                                    return x[0].includes("Run ");
                                return true;
                            }).map(jobId => (
                                <>
                                    <thead>
                                    <tr>
                                        <JobHeader colSpan={6}>
                                            <ShapeSquareIcon16Regular/>{" "}
                                            <Link className="no-underline" to={`/test-analytics/jobs/${jobId[0]}`}>
                                                {jobId[0]}
                                            </Link>
                                        </JobHeader>
                                    </tr>
                                    </thead>
                                    <tbody>
                                    {allJobRuns
                                        .filter(x => x[0] === jobId[0])
                                        .sort((a,b) => Number(b[1]) - Number(a[1]))
                                        .map(x => (
                                            <tr>
                                                <PaddingCell/>
                                                <NumberCell>
                                                    <Link to={getLinkToJob(x[1], x[3])}>#{x[1]}</Link>
                                                </NumberCell>
                                                <BranchCell branch={x[2]}>
                                                    <ShareNetworkIcon/> {x[2]}
                                                </BranchCell>
                                                <CountCell>
                                                    <JobLinkWithResults state={x[11]} to={`/test-analytics/jobs/${jobId}/runs/${x[1]}`}>
                                                        {getText(x[5], x[8], x[9], x[10], x[11], x[12])}
                                                    </JobLinkWithResults>
                                                </CountCell>
                                                <StartedCell>{toLocalTimeFromUtc(x[4])}</StartedCell>
                                                <DurationCell>{formatTestDuration(x[7])}</DurationCell>
                                            </tr>
                                        ))}
                                    </tbody>
                                </>
                            ))}
                        </JobList></>))}
            </ColumnStack>
        </Root>
    );
}

const Root = styled.main`
    max-width: 1000px;
    margin: 24px auto;
`;

const JobList = styled.table`
    width: 100%;
    font-size: 14px;

    td {
        text-align: left;
        padding: 6px 8px;
    }

    thead > tr > th {
        padding-top: 16px;
    }
`;

const JobHeader = styled.th`
    margin-top: 8px;
    text-align: left;

    a {
        font-weight: 600;
    }
`;

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;

const Header2 = styled.h2`
    font-size: 24px;
    line-height: 32px;
    margin-top: 16px;
`;

const Header3 = styled.h3`
    font-size: 22px;
    line-height: 20px;
    margin-top: 16px;
`;

const NumberCell = styled.td`
    width: 80px;
`;

const PaddingCell = styled.td`
    padding: 0 !important;
    width: 12px;
`;

const CountCell = styled.td`
    max-width: 300px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const StartedCell = styled.td`
    max-width: 150px;
    white-space: nowrap;
`;

const DurationCell = styled.td`
    max-width: 140px;
    white-space: nowrap;
`;