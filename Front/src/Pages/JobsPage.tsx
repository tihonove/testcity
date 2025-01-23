import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { JobsQueryRow, JobsView } from "../Components/JobsView";
import { ProjectComboBox } from "../Components/ProjectComboBox";
import { BranchSelect } from "../TestHistory/BranchSelect";
import { getProjectNameById, useSearchParamAsState } from "../Utils";

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

    const allJobs = client
        .useData2<
            [string, string]
        >(`SELECT DISTINCT JobId, ProjectId FROM JobInfo WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW());`, ["2"])
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
                        {!projectId && (
                            <Fit>
                                <ProjectComboBox value={currentGroup} items={allGroup} handler={setCurrentGroup} />
                            </Fit>
                        )}
                        <Fit>
                            <BranchSelect
                                branch={currentBranchName}
                                onChangeBranch={setCurrentBranchName}
                                branchQuery={`SELECT DISTINCT BranchName
                                              FROM JobInfo
                                              WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND BranchName != '' ${projectId ? " AND ProjectId == '" + projectId + "'" : ""}
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
                                    <Header3>{getProjectNameById(section)}</Header3>
                                </Link>
                            </Fit>
                            <JobsView allJobs={allJobs} projectId={section} allJobRuns={allJobRuns} />
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

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;

const Header3 = styled.h3`
    font-size: 22px;
    line-height: 20px;
    margin-top: 16px;
`;
