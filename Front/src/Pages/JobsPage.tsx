import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { JobsQueryRow, JobsView } from "../Components/JobsView";
import { ProjectComboBox } from "../Components/ProjectComboBox";
import { JobsMiniTree } from "../Components/JobsMiniTree";
import { BranchSelect } from "../TestHistory/BranchSelect";
import { getProjectNameById, useSearchParamAsState } from "../Utils";
import { Button, Gapped, Sticky } from "@skbkontur/react-ui";

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

    const allGroup = (projectId != "" ? [projectId] : [...serverProjects.map(x => x[0]), "Utilities"]).sort((a, b) => {
        const isPriorityA = a === "17358" ? -1 : a === "19371" ? -2 : 0;
        const isPriorityB = b === "17358" ? -1 : b === "19371" ? -2 : 0;
        return isPriorityA - isPriorityB || a.localeCompare(b);
    });

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
                JobUrl,
                ProjectId
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
        <Root verticalAlign="top">
            <Sticky side="top" offset={45}>
                <JobsMiniTree allJobs={allJobs} allJobRuns={allJobRuns} allGroup={allGroup} />
            </Sticky>
            <TestListRoot vertical>
                <Header>Jobs</Header>
                <Gapped>
                    {!projectId && (
                        <ProjectComboBox value={currentGroup} items={allGroup} handler={setCurrentGroup} />
                    )}
                    <BranchSelect
                        branch={currentBranchName}
                        onChangeBranch={setCurrentBranchName}
                        branchQuery={`SELECT DISTINCT BranchName
                                            FROM JobInfo
                                            WHERE StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND BranchName != '' ${projectId ? " AND ProjectId == '" + projectId + "'" : ""}
                                            ORDER BY StartDateTime DESC;`}
                    />
                </Gapped>
                {allGroup
                    .filter(s => !currentGroup || s === currentGroup)
                    .map(section => (
                        <React.Fragment key={section}>
                            <SectionTitle gap={24} verticalAlign="middle">
                                <Link
                                    className="no-underline"
                                    to={`/test-analytics/projects/${encodeURIComponent(section)}`}>
                                    <Header3>{getProjectNameById(section)}</Header3>
                                </Link>
                                <Button component="a"
                                    href={`${allJobRuns.find(j => j[14] === section)?.[13].split("/-/")[0]}/-/pipelines/new${currentBranchName ? `?ref=${encodeURIComponent(currentBranchName)}` : ""}`}
                                    target="_blank">
                                    New pipeline
                                </Button>
                            </SectionTitle>
                            <JobsView allJobs={allJobs} projectId={section} allJobRuns={allJobRuns} />
                        </React.Fragment>
                    ))}
            </TestListRoot>
        </Root>
    );
}

const TestListRoot = styled(Gapped)`
    max-width: 1000px;
`;

const SectionTitle = styled(Gapped)`
    margin-top: 16px;
`;

const Root = styled(Gapped)`
    margin: 24px auto;
`;

const Header = styled.h1`
    font-size: 32px;
    line-height: 40px;
`;

const Header3 = styled.h3`
    font-size: 22px;
    line-height: 24px;
`;