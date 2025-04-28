import { ShapeSquareIcon32Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { Paging, Tooltip } from "@skbkontur/react-ui";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchBox } from "../Components/BranchBox";
import { BranchSelect } from "../Components/BranchSelect";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { CommitChanges } from "../Components/CommitChanges";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { JobLink } from "../Components/JobLink";
import { RotatingSpinner } from "../Components/RotatingSpinner";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { useUrlBasedPaging } from "../Components/useUrlBasedPaging";
import { createLinkToJobRun } from "../Domain/Navigation";
import { JobRunNames } from "../Domain/Storage/JobsQuery";
import { formatTestDuration, getOffsetTitle, getText, toLocalTimeFromUtc, useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { reject } from "../Utils/TypeHelpers";
import styles from "./JobRunsPage.module.css";

export function JobRunsPage(): React.JSX.Element {
    const { jobId = "" } = useParams();
    const { groupNodes, rootGroup } = useProjectContextFromUrlParams();
    const project = groupNodes[groupNodes.length - 1] ?? reject("Project not found");

    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);

    const [page, setPage] = useUrlBasedPaging();
    const jobRuns = useStorageQuery(
        x => x.findAllJobsRunsPerJobId(project.id, jobId, currentBranchName, page),
        [project.id, jobId, currentBranchName, page]
    );
    const [isPending, startTransition, isFading] = useDelayedTransition();

    return (
        <ColumnStack block stretch gap={4}>
            <Fit>
                <GroupBreadcrumps branchName={currentBranchName} nodes={groupNodes} />
            </Fit>
            <Fit>
                <h1 className={styles.header1}>
                    <ShapeSquareIcon32Regular /> {jobId}
                </h1>
            </Fit>
            <Fit>
                <BranchSelect branch={currentBranchName} onChangeBranch={setCurrentBranchName} jobId={jobId} />
            </Fit>
            <Fit>
                <SuspenseFadingWrapper fading={isFading}>
                    <table className={styles.runList}>
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>branch</th>
                                <th></th>
                                <th>changes</th>
                                <th>started {getOffsetTitle()}</th>
                                <th>duration</th>
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
                                    <td className={styles.startedCell}>
                                        {toLocalTimeFromUtc(x[JobRunNames.StartDateTime])}
                                    </td>
                                    <td className={styles.durationCell}>
                                        {formatTestDuration(x[JobRunNames.Duration]?.toString() ?? "0")}
                                    </td>
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
            </Fit>
        </ColumnStack>
    );
}
