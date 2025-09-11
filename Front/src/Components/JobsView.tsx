import {
    FileTypeMarkupIcon16Regular,
    SearchLoupePlusIcon16Solid,
    ShapeSquareIcon16Regular,
    ShapeSquareIcon16Solid,
} from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import { BranchCell, SelectedOnHoverTr } from "./Cells";
import { CommitChanges } from "./CommitChanges";
import { createLinkToJob, createLinkToJobRun } from "../Domain/Navigation";
import { getLinkToJob, getText } from "../Utils";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobRunNames, JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { SubIcon } from "./SubIcon";
import { Hint } from "@skbkontur/react-ui";
import { RunsTable } from "../Pages/ProjectsWithRunsTable";
import { stableGroupBy } from "../Utils/ArrayUtils";
import { BranchBox } from "./BranchBox";
import { JobLink } from "./JobLink";
import { TimingCell } from "./TimingCell";
import { RotatingSpinner } from "./RotatingSpinner";
import styles from "./JobsView.module.css";

interface JobsViewProps {
    groupNodes: (GroupNode | Project)[];
    project: Project;
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
                                <th
                                    className={styles.jobHeader}
                                    colSpan={RunsTable.columnCount}
                                    style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
                                    {hasFailedRuns ? (
                                        <ShapeSquareIcon16Solid color="var(--failed-text-color)" />
                                    ) : (
                                        <ShapeSquareIcon16Regular color="var(--success-text-color)" />
                                    )}{" "}
                                    <Link
                                        to={createLinkToJob(rootProjectStructure, projectId, jobId, currentBranchName)}>
                                        {jobId}
                                    </Link>
                                </th>
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
                                            <td
                                                className={styles.paddingCell}
                                                style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}
                                            />
                                            <td className={styles.numberCell}>
                                                <Link
                                                    to={
                                                        x[JobRunNames.JobUrl] ||
                                                        getLinkToJob(x[JobRunNames.JobRunId], x[JobRunNames.ProjectId])
                                                    }>
                                                    #{x[JobRunNames.JobRunId]}
                                                </Link>
                                            </td>
                                            <BranchCell>
                                                <BranchBox name={x[JobRunNames.BranchName]} />
                                            </BranchCell>
                                            <td className={styles.countCell}>
                                                {x[JobRunNames.State] === "Running" ? (
                                                    <>
                                                        <RotatingSpinner /> Running...
                                                    </>
                                                ) : (
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
                                                            x[JobRunNames.TotalTestsCount]?.toString() ?? "0",
                                                            x[JobRunNames.SuccessTestsCount]?.toString() ?? "0",
                                                            x[JobRunNames.SkippedTestsCount]?.toString() ?? "0",
                                                            x[JobRunNames.FailedTestsCount]?.toString() ?? "0",
                                                            x[JobRunNames.State],
                                                            x[JobRunNames.CustomStatusMessage],
                                                            x[JobRunNames.HasCodeQualityReport]
                                                        )}
                                                    </JobLink>
                                                )}
                                            </td>
                                            <td className={styles.changesCell}>
                                                <CommitChanges
                                                    totalCoveredCommitCount={x[JobRunNames.TotalCoveredCommitCount]}
                                                    coveredCommits={x[JobRunNames.CoveredCommits] || []}
                                                />
                                            </td>
                                            <TimingCell
                                                startDateTime={x[JobRunNames.StartDateTime]}
                                                duration={x[JobRunNames.Duration]}
                                            />
                                            <td className={styles.attributesCell}>
                                                {x[JobRunNames.HasCodeQualityReport] != 0 && (
                                                    <Hint text="Code quality report available">
                                                        <SubIcon sub={<SearchLoupePlusIcon16Solid />}>
                                                            <FileTypeMarkupIcon16Regular />
                                                        </SubIcon>
                                                    </Hint>
                                                )}
                                            </td>
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
                                <th
                                    className={styles.jobHeader}
                                    colSpan={RunsTable.columnCount}
                                    style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
                                    <ShapeSquareIcon16Regular color="var(--muted-text-color)" />{" "}
                                    <Link
                                        className="no-underline"
                                        to={createLinkToJob(rootProjectStructure, projectId, jobId, currentBranchName)}>
                                        {jobId}
                                    </Link>
                                </th>
                            </tr>
                        </thead>
                    </React.Fragment>
                );
            })}
        </>
    );
}
