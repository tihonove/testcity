import * as React from "react";
import { Link, useParams } from "react-router-dom";
import { useSearchParamAsState } from "../Utils";
import { BranchSelect } from "../TestHistory/BranchSelect";
import { LogoMicrosoftIcon, QuestionCircleIcon, ShapeSquareIcon32Regular, ShareNetworkIcon } from "@skbkontur/icons";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import styled from "styled-components";

export function JobRunsPage(): React.JSX.Element {
    const { jobId } = useParams();
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    const client = useClickhouseClient();

    const jobRuns = client.useData2<[string, string, string, string, string, string, string]>(
        `
        SELECT 
            JobId,
            JobRunId,
            first_value(b.BranchName) AS BranchName,
            first_value(b.AgentName)  AS AgentName,
            min(b.StartDateTime)      AS StartDateTime,
            count(b.TestId)           AS TotalTestCount,
            first_value(b.AgentOSName)  AS AgentOSName
        FROM TestRunsByRun b
        WHERE b.JobId = '${jobId}' ${currentBranchName ? ` AND b.BranchName = '${currentBranchName}'` : ""}
        GROUP BY JobId, JobRunId
        ORDER BY StartDateTime DESC
        LIMIT 100
        `,
        [currentBranchName]
    );

    return (
        <ColumnStack block stretch gap={2}>
            <Fit>
                <Header1>
                    <ShapeSquareIcon32Regular /> {jobId}
                </Header1>
            </Fit>
            <Fit>
                <BranchSelect
                    branch={currentBranchName}
                    onChangeBranch={setCurrentBranchName}
                    branchQuery={`SELECT DISTINCT BranchName FROM TestRuns WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`}
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
                            <th>started</th>
                        </tr>
                    </thead>
                    <tbody>
                        {jobRuns.map(x => (
                            <tr>
                                <NumberCell>
                                    <Link to={`/test-analytics/jobs/${jobId}/runs/${x[1]}`}>#{x[1]}</Link>
                                </NumberCell>
                                <BranchCell>
                                    <ShareNetworkIcon /> {x[2]}
                                </BranchCell>
                                <CountCell>Total count: {x[5]}</CountCell>
                                <AgentCell>
                                    {/windows/.test(x[6]) ? <LogoMicrosoftIcon /> : <QuestionCircleIcon />} {x[3]}
                                </AgentCell>
                                <StartedCell>{x[4]}</StartedCell>
                            </tr>
                        ))}
                    </tbody>
                </RunList>
            </Fit>
        </ColumnStack>
    );
}

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

const AgentCell = styled.td``;

const StartedCell = styled.td``;
