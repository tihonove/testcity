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
import { BranchCell, JobLinkWithResults, SelectedOnHoverTr } from "./Cells";
import { createLinkToJob, createLinkToJobRun } from "../Domain/Navigation";
import { formatTestDuration, getLinkToJob, getText, toLocalTimeFromUtc } from "../Utils";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobRunNames, JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { GroupNode } from "../Domain/Storage/Storage";
import { SubIcon } from "./SubIcon";
import { Hint } from "@skbkontur/react-ui";
import { RunsTable } from "../Pages/ProjectsWithRunsTable";
import { stableGroupBy } from "../Utils/ArrayUtils";
import { BranchBox } from "./BranchBox";

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
                                                <JobLinkWithResults
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
                                                </JobLinkWithResults>
                                            </CountCell>
                                            <StartedCell>{toLocalTimeFromUtc(x[4])}</StartedCell>
                                            <DurationCell>{formatTestDuration(x[7])}</DurationCell>
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

const StartedCell = styled.td`
    max-width: 150px;
    white-space: nowrap;
`;

const DurationCell = styled.td`
    max-width: 140px;
    white-space: nowrap;
    text-align: right;
`;

const AttributesCell = styled.td`
    max-width: 80px;
    white-space: nowrap;
    text-align: left;
`;
