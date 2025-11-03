import {
    ArrowShapeTriangleADownIcon20Light,
    ArrowShapeTriangleARightIcon20Light,
    FileTypeMarkupIcon16Regular,
    SearchLoupePlusIcon16Solid,
    ShapeSquareIcon16Regular,
    ShapeSquareIcon16Solid,
} from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import { BranchCell, SelectedOnHoverTr } from "./Cells";
import { CommitChanges } from "./CommitChanges";
import { createLinkToJob2, createLinkToJobRun2 } from "../Domain/Navigation";
import { getLinkToJob, getText } from "../Utils";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobRunNames, JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { SubIcon } from "./SubIcon";
import { Hint } from "@skbkontur/react-ui";
import { RunsTable } from "../Pages/ProjectsDashboardPage/Components/ProjectsWithRunsTable";
import { BranchBox } from "./BranchBox";
import { JobLink } from "./JobLink";
import { TimingCell } from "./TimingCell";
import { RotatingSpinner } from "./RotatingSpinner";
import { useUserSettings } from "../Utils/useUserSettings";
import styles from "./JobRunsTable.module.css";
import { Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import { ProjectDashboardNode } from "../Domain/ProjectDashboardNode";

interface JobRunsTableProps {
    project: ProjectDashboardNode;
    job: JobIdWithParentProject;
    jobRuns: JobsQueryRow[];
    currentBranchName?: string;
    indentLevel: number;
    hideRuns?: boolean;
}

export function JobRunsTable({ project, job, jobRuns, currentBranchName, indentLevel, hideRuns }: JobRunsTableProps) {
    const jobId = job[JobIdWithParentProjectNames.JobId];

    const hasFailedRuns = jobRuns.some(x => x[JobRunNames.State] != "Success" && x[JobRunNames.State] != "Canceled");
    const [collapsed, setCollapsed] = useUserSettings(
        ["ui", ...project.fullPathSlug.map(x => x.id), jobId, "collapsed"],
        false
    );

    return (
        <>
            <thead>
                <tr>
                    <th
                        className={styles.jobHeader}
                        colSpan={RunsTable.columnCount}
                        style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
                        <RowStack gap={2} baseline block className={styles.jobHeaderRow}>
                            <Fixed width={20}>
                                <button
                                    className={styles.iconButton}
                                    type="button"
                                    onClick={() => {
                                        setCollapsed(!collapsed);
                                        return false;
                                    }}>
                                    {collapsed ? (
                                        <ArrowShapeTriangleARightIcon20Light />
                                    ) : (
                                        <ArrowShapeTriangleADownIcon20Light />
                                    )}
                                </button>
                            </Fixed>
                            <Fit>
                                {hasFailedRuns ? (
                                    <ShapeSquareIcon16Solid color="var(--failed-text-color)" />
                                ) : (
                                    <ShapeSquareIcon16Regular color="var(--success-text-color)" />
                                )}
                            </Fit>
                            <Fit>
                                <Link to={createLinkToJob2(project.link, jobId, currentBranchName)}>{jobId}</Link>
                            </Fit>
                        </RowStack>
                    </th>
                </tr>
            </thead>
            {!hideRuns && !collapsed && (
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
                                            to={createLinkToJobRun2(
                                                project.link,
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
        </>
    );
}
