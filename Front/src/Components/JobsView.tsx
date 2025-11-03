import { ShapeSquareIcon16Regular } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import { createLinkToJob2 } from "../Domain/Navigation";
import { JobDashboardInfo, ProjectDashboardNode } from "../Domain/ProjectDashboardNode";
import { RunsTable } from "../Pages/ProjectsDashboardPage/Components/ProjectsWithRunsTable";
import { stableGroupBy } from "../Utils/ArrayUtils";
import { JobRunsTable } from "./JobRunsTable";
import styles from "./JobsView.module.css";

interface JobsViewProps {
    project: ProjectDashboardNode;
    hideRuns?: boolean;
    currentBranchName?: string;
    jobs: JobDashboardInfo[];
    indentLevel: number;
}

export function JobsView({ project, jobs, hideRuns, currentBranchName, indentLevel }: JobsViewProps) {
    const groupedJobs = stableGroupBy(jobs, item => item.runs.length > 0);

    const jobsWithRuns = groupedJobs.get(true) || [];
    const jobsWithoutRuns = groupedJobs.get(false) || [];

    return (
        <>
            {jobsWithRuns.map(({ jobId, runs }) => (
                <JobRunsTable
                    project={project}
                    key={jobId + project.id}
                    job={[jobId, project.id]}
                    jobRuns={runs}
                    currentBranchName={currentBranchName}
                    indentLevel={indentLevel}
                    hideRuns={hideRuns}
                />
            ))}

            {jobsWithoutRuns.map(({ jobId }) => {
                return (
                    <React.Fragment key={jobId + project.id}>
                        <thead>
                            <tr>
                                <th
                                    className={styles.jobHeader}
                                    colSpan={RunsTable.columnCount}
                                    style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }}>
                                    <ShapeSquareIcon16Regular color="var(--muted-text-color)" />{" "}
                                    <Link
                                        className="no-underline"
                                        to={createLinkToJob2(project.link, jobId, currentBranchName)}>
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
