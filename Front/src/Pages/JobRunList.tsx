import { Paging } from "@skbkontur/react-ui";
import * as React from "react";
import { Link } from "react-router-dom";
import { BranchBox } from "../Components/BranchBox";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { CommitChanges } from "../Components/CommitChanges";
import { JobLink } from "../Components/JobLink";
import { RotatingSpinner } from "../Components/RotatingSpinner";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useUrlBasedPaging } from "../Components/useUrlBasedPaging";
import { createLinkToJobRun } from "../Domain/Navigation";
import { getText } from "../Utils";
import styles from "./JobRunList.module.css";
import { GroupNode } from "../Domain/Storage/Projects/GroupNode";
import { TimingCell } from "../Components/TimingCell";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";

interface JobRunListProps {
    rootGroup: GroupNode;
    pathToGroup: string[];
    jobId: string;
    branchName: string | undefined;
}

export function JobRunList({ rootGroup, pathToGroup, jobId, branchName }: JobRunListProps) {
    const [page, setPage] = useUrlBasedPaging();
    const jobRuns = useTestCityRequest(
        x => x.runs.getJobRuns(pathToGroup, jobId, branchName, page),
        [pathToGroup, jobId, branchName, page]
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
                        <SelectedOnHoverTr key={x.jobRunId}>
                            <NumberCell>
                                <Link to={x.jobUrl}>#{x.jobRunId}</Link>
                            </NumberCell>
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
                                        to={createLinkToJobRun(rootGroup, x.projectId, jobId, x.jobRunId, branchName)}>
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
                            <TimingCell duration={x.duration?.toString() ?? "0"} startDateTime={x.startDateTime} />
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
