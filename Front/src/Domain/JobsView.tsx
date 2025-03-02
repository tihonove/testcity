import { ShapeSquareIcon16Regular, ShapeSquareIcon16Solid, ShareNetworkIcon } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import styled, { useTheme } from "styled-components";
import { BranchCell, JobLinkWithResults, SelectedOnHoverTr } from "../Components/Cells";
import { createLinkToJob, createLinkToJobRun } from "./Navigation";
import { formatTestDuration, getLinkToJob, getText, toLocalTimeFromUtc } from "../Utils";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "./JobIdWithParentProject";
import { JobRunNames, JobsQueryRow } from "./JobsQueryRow";
import { GroupNode } from "./Storage";

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

    return (
        <>
            {allJobs.map(job => {
                const jobId = job[JobIdWithParentProjectNames.JobId];
                const projectId = job[JobIdWithParentProjectNames.ProjectId];
                const jonRuns = allJobRuns.filter(
                    x => x[JobRunNames.JobId] === jobId && x[JobRunNames.ProjectId] === projectId
                );
                const hasFailedRuns = jonRuns.some(
                    x => x[JobRunNames.State] != "Success" && x[JobRunNames.State] != "Canceled"
                );
                return (
                    <React.Fragment key={jobId + projectId}>
                        <thead>
                            <tr>
                                <JobHeader colSpan={6} style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
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
                                {jonRuns
                                    .sort((a, b) => Number(b[1]) - Number(a[1]))
                                    .map(x => (
                                        <SelectedOnHoverTr key={x[1]}>
                                            <PaddingCell style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }} />
                                            <NumberCell>
                                                <Link to={x[13] || getLinkToJob(x[1], x[3])}>#{x[1]}</Link>
                                            </NumberCell>
                                            <BranchCell $defaultBranch={x[JobRunNames.BranchName] == "master"}>
                                                <ShareNetworkIcon /> {x[JobRunNames.BranchName]}
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
                                                    {getText(x[5], x[8], x[9], x[10], x[11], x[12])}
                                                </JobLinkWithResults>
                                            </CountCell>
                                            <StartedCell>{toLocalTimeFromUtc(x[4])}</StartedCell>
                                            <DurationCell>{formatTestDuration(x[7])}</DurationCell>
                                        </SelectedOnHoverTr>
                                    ))}
                            </tbody>
                        )}
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
