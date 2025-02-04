import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { JobsView } from "../Domain/JobsView";
import { JobsQueryRow } from "../Domain/JobsQueryRow";

export function MergeRequestJobsPage(): React.JSX.Element {
    const { projectId = "", gitLabMergeRequestId = "" } = useParams();
    const client = useClickhouseClient();
    const branchName = `refs/merge-requests/${gitLabMergeRequestId}/head`;

    const allJobs = client
        .useData2<
            [string, string]
        >(`SELECT DISTINCT JobId, ProjectId FROM JobInfo WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND ProjectId == '${projectId}';`)
        .filter(x => x[0].trim() !== "");

    const allJobRuns = client.useData2<JobsQueryRow>(
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
                CustomStatusMessage,
                JobUrl
            FROM (
                     SELECT
                         *,
                         ROW_NUMBER() OVER (PARTITION BY JobId, BranchName ORDER BY StartDateTime DESC) AS rn
                     FROM JobInfo
                     WHERE StartDateTime >= now() - INTERVAL 30 DAY AND BranchName = '${branchName}'
                     ) AS filtered
            WHERE rn = 1
            ORDER BY JobId, StartDateTime DESC;`,
        [branchName]
    );

    return (
        <Root>
            <ColumnStack block stretch gap={2}>
                <Fit>
                    <Header>Merge request #{gitLabMergeRequestId} runs</Header>
                </Fit>
                <Fit>
                    <JobsView allJobs={allJobs} projectId={projectId} allJobRuns={allJobRuns} />
                </Fit>
            </ColumnStack>
        </Root>
    );
}

const Root = styled.main`
    max-width: 1000px;
    margin: 24px auto;
`;

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;
