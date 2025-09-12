import { ShapeSquareIcon16Regular } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import { createLinkToJob } from "../Domain/Navigation";
import { JobIdWithParentProject, JobIdWithParentProjectNames } from "../Domain/JobIdWithParentProject";
import { JobRunNames, JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { RunsTable } from "../Pages/ProjectsWithRunsTable";
import { stableGroupBy } from "../Utils/ArrayUtils";
import { JobRunsTable } from "./JobRunsTable";
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
    groupNodes,
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
            {jobsWithRuns.map(({ job, jobRuns }) => (
                <JobRunsTable
                    groupNodes={groupNodes}
                    key={job[JobIdWithParentProjectNames.JobId] + job[JobIdWithParentProjectNames.ProjectId]}
                    job={job}
                    jobRuns={jobRuns}
                    rootProjectStructure={rootProjectStructure}
                    currentBranchName={currentBranchName}
                    indentLevel={indentLevel}
                    hideRuns={hideRuns}
                />
            ))}

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
