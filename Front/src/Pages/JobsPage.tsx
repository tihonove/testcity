import {ColumnStack, Fit, RowStack} from "@skbkontur/react-stack-layout";
import * as React from "react";
import styled from "styled-components";
import {
    LogoMicrosoftIcon,
    QuestionCircleIcon,
    ShapeSquareIcon16Regular,
    ShareNetworkIcon,
} from "@skbkontur/icons";
import {useClickhouseClient} from "../ClickhouseClientHooksWrapper";
import {BranchSelect} from "../TestHistory/BranchSelect";
import {useSearchParamAsState} from "../Utils";
import {Link} from "react-router-dom";

export function JobsPage(): React.JSX.Element {
    const client = useClickhouseClient();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");

    const sections = ["Wolfs", "Forms mastering", "Utilities"]
    const allJobs = client
        .useData2<[string]>(`SELECT DISTINCT JobId FROM TestRuns WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`)
        .filter(x => x[0] != null && x[0].trim() !== "");

    const allJobRuns = client.useData2<[string, string, string, string, string, string, string, string, string]>(
        `
            SELECT bb.JobId,
                   aa.JobRunId,
                   bb.BranchName,
                   aa.AgentName,
                   bb.MaxStartDateTime,
                   aa.TestCount,
                   aa.AgentOSName
            FROM (SELECT a.JobId,
                         a.BranchName,
                         max(a.StartDateTime) MaxStartDateTime
                  FROM (SELECT JobId,
                               JobRunId,
                               first_value(b.BranchName) AS BranchName,
                               min(b.StartDateTime)      AS StartDateTime
                        FROM TestRunsByRun b
                        where b.StartDateTime >= DATE_ADD(DAY, -3, NOW()) ${currentBranchName ? ` AND b.BranchName = '${currentBranchName}'` : ""}
                        GROUP BY JobId,
                            JobRunId) AS a
                  WHERE a.StartDateTime >= DATE_ADD(DAY, -3, now())
                  GROUP BY a.JobId, a.BranchName) bb
                     INNER JOIN (SELECT JobId,
                                        JobRunId,
                                        count(z.TestId)            as TestCount,
                                        first_value(z.BranchName)  AS BranchName,
                                        first_value(z.AgentName)   AS AgentName,
                                        first_value(z.AgentOSName) AS AgentOSName,
                                        min(z.StartDateTime)       AS StartDateTime
                                 FROM TestRunsByRun z
                                 where z.StartDateTime >= DATE_ADD(DAY, -3, NOW()) ${currentBranchName ? ` AND z.BranchName = '${currentBranchName}'` : ""}
                                 GROUP BY JobId,
                                     JobRunId) AS aa
                                ON aa.JobId == bb.JobId and aa.BranchName == bb.BranchName and
                       aa.StartDateTime == bb.MaxStartDateTime`,
        ["1", currentBranchName]
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
                            <Header2>All jobs</Header2>
                        </Fit>
                        <Fit>
                            <BranchSelect
                                branch={currentBranchName}
                                onChangeBranch={setCurrentBranchName}
                                branchQuery={`SELECT DISTINCT BranchName FROM TestRuns WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`}
                            />
                        </Fit>
                    </RowStack>
                </Fit>
                <JobList>
                    {sections.map(section => (
                        <>
                            <Fit>
                                <Header3>{section}</Header3>
                            </Fit>
                            <>{allJobs.filter(x => {
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
                                        .map(x => (
                                            <tr>
                                                <PaddingCell/>
                                                <NumberCell>
                                                    <Link
                                                        className="no-underline"
                                                        to={`/test-analytics/jobs/${jobId}/runs/${x[1]}`}>
                                                        #{x[1]}
                                                    </Link>
                                                </NumberCell>
                                                <BranchCell>
                                                    <ShareNetworkIcon/> {x[2]}
                                                </BranchCell>
                                                <CountCell>Total count: {x[5]}</CountCell>
                                                <AgentCell>
                                                    {/windows/.test(x[6]) ? <LogoMicrosoftIcon/> : <QuestionCircleIcon/>}{" "}
                                                    {x[3]}
                                                </AgentCell>
                                                <StartedCell>{x[4]}</StartedCell>
                                            </tr>
                                        ))}
                                    </tbody>
                                </>
                            ))}</>
                        </>))}
                </JobList>
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
    font-size: 16px;
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

const BranchCell = styled.td`
    max-width: 200px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const CountCell = styled.td`
    max-width: 200px;
    white-space: nowrap;
`;

const AgentCell = styled.td`
    max-width: 200px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const StartedCell = styled.td`
    max-width: 140px;
    white-space: nowrap;
`;
