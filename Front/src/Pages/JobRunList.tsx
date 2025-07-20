import { Paging } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchBox } from "../Components/BranchBox";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { CommitChanges } from "../Components/CommitChanges";
import { JobLink } from "../Components/JobLink";
import { RotatingSpinner } from "../Components/RotatingSpinner";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useUrlBasedPaging } from "../Components/useUrlBasedPaging";
import { createLinkToJobRun } from "../Domain/Navigation";
import { JobRunNames } from "../Domain/Storage/JobsQuery";
import { formatTestDuration, getOffsetTitle, getText, toLocalTimeFromUtc } from "../Utils";
import styles from "./JobRunList.module.css";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { TimingCell } from "../Components/TimingCell";

interface JobRunListProps {
    rootGroup: GroupNode;
    project: GroupNode | Project;
    jobId: string;
    branchName: string | undefined;
}

export function JobRunList({ rootGroup, project, jobId, branchName }: JobRunListProps) {
    const [page, setPage] = useUrlBasedPaging();
    const jobRuns = useStorageQuery(
        x => x.findAllJobsRunsPerJobId(project.id, jobId, branchName, page),
        [project.id, jobId, branchName, page]
    );
    const [isPending, startTransition, isFading] = useDelayedTransition();

    return (
        <SuspenseFadingWrapper fading={isFading}>
            <table className={styles.runList}>
                <thead>
                    <tr>
                        <th>#</th>
                        <th>branch</th>
                        <th></th>
                        <th>changes</th>
                        <th>duration / started</th>
                    </tr>
                </thead>
                <tbody>
                    {jobRuns.map(x => (
                        <SelectedOnHoverTr key={x[JobRunNames.JobRunId]}>
                            <NumberCell>
                                <Link to={x[13]}>#{x[1]}</Link>
                            </NumberCell>
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
                                        state={x[11]}
                                        to={createLinkToJobRun(
                                            rootGroup,
                                            project.id,
                                            jobId,
                                            x[JobRunNames.JobRunId],
                                            branchName
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
                                duration={x[JobRunNames.Duration]?.toString() ?? "0"}
                                startDateTime={x[JobRunNames.StartDateTime]}
                            />
                        </SelectedOnHoverTr>
                    ))}
                </tbody>
            </table>
            <Paging
                activePage={page + 1}
                onPageChange={x => {
                    startTransition(() => {
                        setPage(x - 1);
                    });
                }}
                pagesCount={100}
            />
        </SuspenseFadingWrapper>
    );
}
