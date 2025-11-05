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
import { createLinkToJob2, createLinkToJobRun2, createLinkToProject } from "../Domain/Navigation";
import { getLinkToJob, getText } from "../Utils";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
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
import { JobRun, ProjectDashboardNode } from "../Domain/ProjectDashboardNode";

interface JobRunsTableProps {
    project: ProjectDashboardNode;
    job: JobIdWithParentProject;
    jobRuns: JobRun[];
    currentBranchName?: string;
    indentLevel: number;
    hideRuns?: boolean;
}

export function JobRunsTable({ project, job, jobRuns, currentBranchName, indentLevel, hideRuns }: JobRunsTableProps) {
    const jobId = job[JobIdWithParentProjectNames.JobId];

    const hasFailedRuns = jobRuns.some(x => x.state !== "Success" && x.state !== "Canceled");
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
                                <Link
                                    to={createLinkToJob2(
                                        createLinkToProject(project.fullPathSlug),
                                        jobId,
                                        currentBranchName
                                    )}>
                                    {jobId}
                                </Link>
                            </Fit>
                        </RowStack>
                    </th>
                </tr>
            </thead>
            {!hideRuns && !collapsed && (
                <tbody>
                    {jobRuns
                        .sort((a, b) => Number(b.projectId) - Number(a.projectId))
                        .map(x => (
                            <SelectedOnHoverTr key={x.jobRunId}>
                                <td
                                    className={styles.paddingCell}
                                    style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}
                                />
                                <td className={styles.numberCell}>
                                    <Link to={x.jobUrl || getLinkToJob(x.jobRunId, x.projectId)}>#{x.jobRunId}</Link>
                                </td>
                                <BranchCell>
                                    <BranchBox name={x.branchName} />
                                </BranchCell>
                                <td className={styles.countCell}>
                                    {x.state === "Running" ? (
                                        <>
                                            <RotatingSpinner /> Running...
                                        </>
                                    ) : (
                                        <JobLink
                                            state={x.state}
                                            to={createLinkToJobRun2(
                                                createLinkToProject(project.fullPathSlug),
                                                jobId,
                                                x.jobRunId,
                                                currentBranchName
                                            )}>
                                            {getText(
                                                x.totalTestsCount?.toString() ?? "0",
                                                x.successTestsCount?.toString() ?? "0",
                                                x.skippedTestsCount?.toString() ?? "0",
                                                x.failedTestsCount?.toString() ?? "0",
                                                x.state,
                                                x.customStatusMessage,
                                                x.hasCodeQualityReport ? 1 : 0
                                            )}
                                        </JobLink>
                                    )}
                                </td>
                                <td className={styles.changesCell}>
                                    <CommitChanges
                                        totalCoveredCommitCount={x.totalCoveredCommitCount}
                                        coveredCommits={x.changesSinceLastRun.map(commit => ({
                                            CommitSha: commit.parentCommitSha,
                                            AuthorName: commit.authorName,
                                            AuthorEmail: commit.authorEmail,
                                            MessagePreview: commit.messagePreview,
                                        }))}
                                    />
                                </td>
                                <TimingCell startDateTime={x.startDateTime} duration={x.duration} />
                                <td className={styles.attributesCell}>
                                    {x.hasCodeQualityReport && (
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
