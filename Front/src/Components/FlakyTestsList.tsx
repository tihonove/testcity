import { Paging } from "@skbkontur/react-ui";
import * as React from "react";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { NumberCell, SelectedOnHoverTr } from "./Cells";
import { SuspenseFadingWrapper, useDelayedTransition } from "./useDelayedTransition";
import { useUrlBasedPaging } from "./useUrlBasedPaging";
import { FlakyTestQueryRowNames } from "../Domain/Storage/FlakyTestQuery";
import { toLocalTimeFromUtc } from "../Utils";
import styles from "./FlakyTestsList.module.css";
import { TestName } from "./TestName";
import { TimeClockFastIcon16Regular } from "@skbkontur/icons";
import { createLinkToTestHistory, useBasePrefix } from "../Domain/Navigation";

interface FlakyTestsListProps {
    pathToProject: string[];
    projectId: string;
    jobId: string;
}

const ITEMS_PER_PAGE = 50;

export function FlakyTestsList({ projectId, jobId, ...props }: FlakyTestsListProps) {
    const basePrefix = useBasePrefix();
    const [page, setPage] = useUrlBasedPaging();
    const flakyTests = useStorageQuery(
        x => x.getFlakyTests(projectId, jobId, ITEMS_PER_PAGE, page * ITEMS_PER_PAGE),
        [projectId, jobId, page]
    );
    const [isPending, startTransition, isFading] = useDelayedTransition();

    const formatFlipRate = (rate: number) => `${(rate * 100).toFixed(1)}%`;

    return (
        <SuspenseFadingWrapper fading={isFading}>
            <table className={styles.flakyTestsList}>
                <thead>
                    <tr className={styles.flakyTestsListHeadRow}>
                        <th>Test name</th>
                        <th></th>
                        <th>Flip Rate</th>
                        <th>Run Count</th>
                        <th>Fail Count</th>
                    </tr>
                </thead>
                <tbody>
                    {flakyTests.map((test, index) => (
                        <SelectedOnHoverTr key={test[FlakyTestQueryRowNames.TestId]}>
                            <td className={styles.testIdCell}>
                                <TestName
                                    onTestNameClick={
                                        () => {}
                                        // testRun[TestRunQueryRowNames.State] === "Failed"
                                        //     ? () => {
                                        //         setExpandOutput(!expandOutput);
                                        //     }
                                        //     : undefined
                                    }
                                    onSetSearchValue={x => {
                                        // onSetSearchTextImmediate(x);
                                    }}
                                    value={test[FlakyTestQueryRowNames.TestId]}
                                />
                            </td>
                            <td className={styles.actionsCell}>
                                <a
                                    href={createLinkToTestHistory(
                                        basePrefix,
                                        test[FlakyTestQueryRowNames.TestId],
                                        props.pathToProject
                                    )}>
                                    <TimeClockFastIcon16Regular /> Show test history
                                </a>
                            </td>
                            <NumberCell>
                                <span className={styles.flipRate}>
                                    {formatFlipRate(test[FlakyTestQueryRowNames.FlipRate])}
                                </span>
                            </NumberCell>
                            <NumberCell>{test[FlakyTestQueryRowNames.RunCount]}</NumberCell>
                            <NumberCell>{test[FlakyTestQueryRowNames.FailCount]}</NumberCell>
                        </SelectedOnHoverTr>
                    ))}
                </tbody>
            </table>
            {flakyTests.length > 0 && (
                <Paging
                    activePage={page + 1}
                    onPageChange={x => {
                        startTransition(() => {
                            setPage(x - 1);
                        });
                    }}
                    pagesCount={Math.max(1, Math.ceil(1000 / ITEMS_PER_PAGE))} // Примерное количество страниц
                />
            )}
            {flakyTests.length === 0 && (
                <div className={styles.emptyState}>
                    <p>Good news! There are no flaky tests.</p>
                </div>
            )}
        </SuspenseFadingWrapper>
    );
}
