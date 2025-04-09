import {
    FileTypeMarkupIcon16Regular,
    SearchLoupeIcon16Solid,
    SearchLoupePlusIcon16Solid,
    ShapeSquareIcon16Regular,
    ShapeSquareIcon16Solid,
    ShareNetworkIcon,
} from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import styled, { useTheme } from "styled-components";
import { BranchCell, SelectedOnHoverTr } from "./Cells";
import { createLinkToJob, createLinkToJobRun } from "../Domain/Navigation";
import { formatTestDuration, getLinkToJob, getText, toLocalTimeFromUtc } from "../Utils";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobRunNames, JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { GroupNode } from "../Domain/Storage/Storage";
import { SubIcon } from "./SubIcon";
import { Hint, Tooltip } from "@skbkontur/react-ui";
import { RunsTable } from "../Pages/ProjectsWithRunsTable";
import { stableGroupBy } from "../Utils/ArrayUtils";
import { BranchBox } from "./BranchBox";
import { JobLink } from "./JobLink";
import { TimingCell } from "./TimingCell";
import { theme } from "../Theme/ITheme";

interface JobsViewProps {
    hideRuns?: boolean;
    currentBranchName?: string;
    rootProjectStructure: GroupNode;
    allJobs: JobIdWithParentProject[];
    allJobRuns: JobsQueryRow[];
    indentLevel: number;
}

export function JobsView({
    rootProjectStructure,
    hideRuns,
    allJobs,
    allJobRuns,
    currentBranchName,
    indentLevel,
}: JobsViewProps) {
    const theme = useTheme();

    const jobsWithTheirRuns = allJobs.map(job => {
        const jobId = job[JobIdWithParentProjectNames.JobId];
        const projectId = job[JobIdWithParentProjectNames.ProjectId];
        const jobRuns = allJobRuns.filter(
            x => x[JobRunNames.JobId] === jobId && x[JobRunNames.ProjectId] === projectId
        );
        return { job, jobRuns };
    });

    const groupedJobs = stableGroupBy(jobsWithTheirRuns, item => item.jobRuns.length > 0);

    const jobsWithRuns = groupedJobs.get(true) || [];
    const jobsWithoutRuns = groupedJobs.get(false) || [];

    return (
        <>
            {jobsWithRuns.map(({ job, jobRuns }) => {
                const jobId = job[JobIdWithParentProjectNames.JobId];
                const projectId = job[JobIdWithParentProjectNames.ProjectId];
                const hasFailedRuns = jobRuns.some(
                    x => x[JobRunNames.State] != "Success" && x[JobRunNames.State] != "Canceled"
                );
                return (
                    <React.Fragment key={jobId + projectId}>
                        <thead>
                            <tr>
                                <JobHeader
                                    colSpan={RunsTable.columnCount}
                                    style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
                                    {hasFailedRuns ? (
                                        <ShapeSquareIcon16Solid color={theme.failedTextColor} />
                                    ) : (
                                        <ShapeSquareIcon16Regular color={theme.successTextColor} />
                                    )}{" "}
                                    <Link
                                        className="no-underline"
                                        to={createLinkToJob(rootProjectStructure, projectId, jobId, currentBranchName)}>
                                        {jobId}
                                    </Link>
                                </JobHeader>
                            </tr>
                        </thead>
                        {!hideRuns && (
                            <tbody>
                                {jobRuns
                                    .sort(
                                        (a, b) =>
                                            Number(b[JobIdWithParentProjectNames.ProjectId]) -
                                            Number(a[JobIdWithParentProjectNames.ProjectId])
                                    )
                                    .map(x => (
                                        <SelectedOnHoverTr key={x[JobRunNames.JobRunId]}>
                                            <PaddingCell style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }} />
                                            <NumberCell>
                                                <Link to={x[13] || getLinkToJob(x[1], x[3])}>#{x[1]}</Link>
                                            </NumberCell>
                                            <BranchCell>
                                                <BranchBox name={x[JobRunNames.BranchName]} />
                                            </BranchCell>
                                            <CountCell>
                                                <JobLink
                                                    state={x[JobRunNames.State]}
                                                    to={createLinkToJobRun(
                                                        rootProjectStructure,
                                                        projectId,
                                                        jobId,
                                                        x[JobRunNames.JobRunId],
                                                        currentBranchName
                                                    )}>
                                                    {getText(
                                                        x[JobRunNames.TotalTestsCount],
                                                        x[JobRunNames.SuccessTestsCount],
                                                        x[JobRunNames.SkippedTestsCount],
                                                        x[JobRunNames.FailedTestsCount],
                                                        x[JobRunNames.State],
                                                        x[JobRunNames.CustomStatusMessage],
                                                        x[JobRunNames.HasCodeQualityReport]
                                                    )}
                                                </JobLink>
                                            </CountCell>
                                            {localStorage.getItem("changes") === "true" && (
                                                <ChangesCell>
                                                    {x[JobRunNames.TotalCoveredCommitCount] > 0 ? (
                                                        <Tooltip
                                                            trigger="click"
                                                            render={() => (
                                                                <ChangesList>
                                                                    <tbody>
                                                                        {x[JobRunNames.CoveredCommits].map(
                                                                            (commit, index) => (
                                                                                <ChangeItem key={index}>
                                                                                    <Message>{commit[3]}</Message>
                                                                                    <Author>
                                                                                        {commit[1]} &lt;{commit[2]}&gt;
                                                                                    </Author>
                                                                                    <Sha>
                                                                                        {commit[0].substring(0, 7)}
                                                                                    </Sha>
                                                                                </ChangeItem>
                                                                            )
                                                                        )}
                                                                    </tbody>
                                                                </ChangesList>
                                                            )}>
                                                            <ChangesLink>
                                                                {x[JobRunNames.TotalCoveredCommitCount]} change
                                                                {x[JobRunNames.TotalCoveredCommitCount] !== 1
                                                                    ? "s"
                                                                    : ""}
                                                            </ChangesLink>
                                                        </Tooltip>
                                                    ) : (
                                                        <NoChanges>No changes</NoChanges>
                                                    )}
                                                </ChangesCell>
                                            )}
                                            <TimingCell startDateTime={x[4]} duration={x[7]} />
                                            <AttributesCell>
                                                {x[JobRunNames.HasCodeQualityReport] != 0 && (
                                                    <Hint text="Code quality report available">
                                                        <SubIcon sub={<SearchLoupePlusIcon16Solid />}>
                                                            <FileTypeMarkupIcon16Regular />
                                                        </SubIcon>
                                                    </Hint>
                                                )}
                                            </AttributesCell>
                                        </SelectedOnHoverTr>
                                    ))}
                            </tbody>
                        )}
                    </React.Fragment>
                );
            })}

            {jobsWithoutRuns.map(({ job }) => {
                const jobId = job[JobIdWithParentProjectNames.JobId];
                const projectId = job[JobIdWithParentProjectNames.ProjectId];
                return (
                    <React.Fragment key={jobId + projectId}>
                        <thead>
                            <tr>
                                <JobHeader
                                    colSpan={RunsTable.columnCount}
                                    style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
                                    <ShapeSquareIcon16Regular color={theme.mutedTextColor} />{" "}
                                    <Link
                                        className="no-underline"
                                        to={createLinkToJob(rootProjectStructure, projectId, jobId, currentBranchName)}>
                                        {jobId}
                                    </Link>
                                </JobHeader>
                            </tr>
                        </thead>
                    </React.Fragment>
                );
            })}
        </>
    );
}

const JobHeader = styled.th`
    margin-top: 8px;
    text-align: left;

    a {
        font-weight: 600;
    }
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

const ChangesCell = styled.td`
    max-width: 300px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const ChangesLink = styled.span`
    cursor: pointer;
    text-decoration: underline;
`;

const AttributesCell = styled.td`
    max-width: 80px;
    white-space: nowrap;
    text-align: left;
`;

const ChangesList = styled.table`
    padding: 10px;
    max-width: 800px;
`;

const ChangeItem = styled.tr``;

const Message = styled.td`
    font-weight: 600;
    max-width: 300px;
    font-size: 12px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const Author = styled.td`
    display: inline-block;
    font-size: 12px;
    padding-left: 5px;
    color: ${theme.mutedTextColor};
`;

const Sha = styled.td`
    font-size: 12px;
    padding-left: 5px;
    color: ${theme.mutedTextColor};
`;

const NoChanges = styled.td`
    color: ${theme.mutedTextColor};
`;
