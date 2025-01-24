import { ColumnStack, Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { JobsQueryRow, JobsView } from "../Components/JobsView";
import { ProjectComboBox } from "../Components/ProjectComboBox";
import { BranchSelect } from "../TestHistory/BranchSelect";
import { getProjectNameById, useSearchParamAsState } from "../Utils";
import { Gapped, Sticky } from "@skbkontur/react-ui";
import { ShapeSquareIcon16Regular } from "@skbkontur/icons";

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
        <Gapped style={{ border: "0px", borderColor: "white", borderStyle: "solid" }} verticalAlign="top">
            <Sticky side="top" getStop={() => null} offset={25}>
                {allGroup.map(section => (
                    <>
                        <Link className="no-underline" to={`/test-analytics/projects/${encodeURIComponent(section)}`}>
                            <Header3>{getProjectNameById(section)}</Header3>
                        </Link>
                        {allJobs
                            .filter(x => (x[1] ? x[1] === section : true))
                            .map(j => (
                                <JobTreeLeaf title={j[0]}>
                                    <ShapeSquareIcon16Regular />{" "}
                                    <Link
                                        className="no-underline"
                                        to={`/test-analytics/jobs/${encodeURIComponent(j[0])}`}>
                                        {j[0]}
                                    </Link>
                                </JobTreeLeaf>
                            ))}
                    </>
                ))}
            </Sticky>
            <TestListRoot vertical style={{ border: "0px", borderColor: "white", borderStyle: "solid" }}>
                <Header>Jobs</Header>
                <Gapped>
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
                </Gapped>
                {allGroup
                    .filter(s => !currentGroup || s === currentGroup)
                    .map(section => (
                        <React.Fragment key={section}>
                            <Link
                                className="no-underline"
                                to={`/test-analytics/projects/${encodeURIComponent(section)}`}>
                                <Header3>{getProjectNameById(section)}</Header3>
                            </Link>
                            <JobsView allJobs={allJobs} projectId={section} allJobRuns={allJobRuns} />
                        </React.Fragment>
                    ))}
            </TestListRoot>
        </Gapped>
    );
}

const TestListRoot = styled(Gapped)`
    max-width: 1000px;
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
    line-height: 20px;
    margin-top: 16px;
`;

const JobTreeLeaf = styled.div`
    margin-left: 16px;
    max-width: 250px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;
