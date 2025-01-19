import { ShapeSquareIcon16Regular, ShareNetworkIcon } from "@skbkontur/icons";
import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { BranchCell, JobLinkWithResults } from "../Components/BranchCell";
import { JobComboBox } from "../Components/JobComboBox";
import { BranchSelect } from "../TestHistory/BranchSelect";
import { formatTestDuration, getLinkToJob, getText, toLocalTimeFromUtc, useSearchParamAsState } from "../Utils";

export function JobsPage(): React.JSX.Element {
    const { projectId = "" } = useParams();
    const client = useClickhouseClient();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    const [currentGroup, setCurrentGroup] = useSearchParamAsState("group");

    const serverProjects = client
        .useData2<
            [string]
        >(`SELECT DISTINCT ProjectId FROM JobInfo WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`, ["1"])
        .filter(x => x[0].trim() !== "");

    const allGroup = projectId != "" ? [projectId] : [...serverProjects.map(x => x[0]), "Utilities"];
    console.log(allGroup);
    const allJobs = client
        .useData2<
            [string, string]
        >(`SELECT DISTINCT JobId, ProjectId FROM JobInfo WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`, ["2"])
        .filter(x => x[0].trim() !== "");

    const allJobRuns = client.useData2<
        [string, string, string, string, string, string, string, string, string, string, string, string, string]
    >(
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
                            <JobComboBox value={currentGroup} items={allGroup} handler={setCurrentGroup} />
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
                        <React.Fragment key={section}>
                            <Fit>
                                <Link
                                    className="no-underline"
                                    to={`/test-analytics/projects/${encodeURIComponent(section)}`}>
                                    <Header3>
                                        {section === "17358"
                                            ? "Wolfs"
                                            : section === "19371"
                                              ? "Forms mastering"
                                              : section === "182"
                                                ? "Diadoc"
                                                : section}
                                    </Header3>
                                </Link>
                            </Fit>
                            <JobList>
                                {allJobs
                                    .filter(x => {
                                        if (x[1]) {
                                            return x[1] === section;
                                        }
                                        if (section === "17358")
                                            return !x[0].includes("FM · ") && !x[0].includes("Run ");
                                        else if (section === "19371") return x[0].includes("FM · ");
                                        else if (section === "Utilities") return x[0].includes("Run ");
                                        return true;
                                    })
                                    .map(jobId => (
                                        <React.Fragment key={jobId[0] + jobId[1]}>
                                            <thead>
                                                <tr>
                                                    <JobHeader colSpan={6}>
                                                        <ShapeSquareIcon16Regular />{" "}
                                                        <Link
                                                            className="no-underline"
                                                            to={`/test-analytics/jobs/${encodeURIComponent(jobId[0])}`}>
                                                            {jobId[0]}
                                                        </Link>
                                                    </JobHeader>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {allJobRuns
                                                    .filter(x => x[0] === jobId[0])
                                                    .sort((a, b) => Number(b[1]) - Number(a[1]))
                                                    .map(x => (
                                                        <tr key={x[1]}>
                                                            <PaddingCell />
                                                            <NumberCell>
                                                                <Link to={getLinkToJob(x[1], x[3])}>#{x[1]}</Link>
                                                            </NumberCell>
                                                            <BranchCell branch={x[2]}>
                                                                <ShareNetworkIcon /> {x[2]}
                                                            </BranchCell>
                                                            <CountCell>
                                                                <JobLinkWithResults
                                                                    state={x[11]}
                                                                    to={`/test-analytics/jobs/${encodeURIComponent(jobId[0])}/runs/${encodeURIComponent(x[1])}`}>
                                                                    {getText(x[5], x[8], x[9], x[10], x[11], x[12])}
                                                                </JobLinkWithResults>
                                                            </CountCell>
                                                            <StartedCell>{toLocalTimeFromUtc(x[4])}</StartedCell>
                                                            <DurationCell>{formatTestDuration(x[7])}</DurationCell>
                                                        </tr>
                                                    ))}
                                            </tbody>
                                        </React.Fragment>
                                    ))}
                            </JobList>
                        </React.Fragment>
                    ))}
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
