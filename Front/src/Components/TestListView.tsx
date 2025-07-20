import { NetDownloadIcon24Regular } from "@skbkontur/icons";
import { Button, Input, Paging, Spinner } from "@skbkontur/react-ui";
import * as React from "react";

import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Suspense, useDeferredValue } from "react";
import { Link } from "react-router-dom";
import { useClickhouseClient, useStorage, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { useBasePrefix } from "../Domain/Navigation";
import { TestRunQueryRowNames } from "../Domain/Storage/TestRunQuery";
import {
    useFilteredValues,
    useSearchParamAsState,
    useSearchParamDebouncedAsState,
    useSearchParamsAsState,
} from "../Utils";
import { SortHeaderLink } from "./SortHeaderLink";
import { TestOutputModal } from "./TestOutputModal";
import { TestRunRow } from "./TestRunRow";
import { TestTypeFilterButton } from "./TestTypeFilterButton";
import { SuspenseFadingWrapper, useDelayedTransition } from "./useDelayedTransition";
import { useUrlBasedPaging } from "./useUrlBasedPaging";
import styles from "./TestListView.module.css";

interface TestListViewProps {
    projectId?: string;
    jobId?: string;
    jobRunIds: string[];
    pathToProject: string[];
    linksBlock?: React.ReactNode;
}

export function TestListView(props: TestListViewProps): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const [isPending, startTransition, isFading] = useDelayedTransition();

    const [testCasesTypeRaw, setTestCasesType] = useSearchParamAsState("type");
    const testCasesType = useFilteredValues(testCasesTypeRaw, ["Success", "Failed", "Skipped"] as const, undefined);
    const [sortFieldRaw, setSortField] = useSearchParamAsState("sort");
    const sortField = useFilteredValues(
        sortFieldRaw,
        ["State", "TestId", "Duration", "StartDateTime"] as const,
        undefined
    );
    const storage = useStorage();
    const [sortDirectionRaw, setSortDirection] = useSearchParamAsState("direction", "desc");
    const sortDirection = useFilteredValues(sortDirectionRaw, ["ASC", "DESC"] as const, undefined);
    const [searchText, setSearchText, debouncedSearchValue = "", setSearchTextImmediate] =
        useSearchParamDebouncedAsState("filter", 500, "");
    const itemsPerPage = 100;
    const [page, setPage] = useUrlBasedPaging();
    const searchValue = useDeferredValue(debouncedSearchValue);

    const [outputModalIds, setOutputModalIds] = useSearchParamsAsState(["oid", "ojobid"]);

    const [testList, stats] = useStorageQuery(
        s => s.getTestListWithStat(props.jobRunIds, sortField, sortDirection, searchValue, testCasesType, 100, page),
        [props.jobRunIds, sortField, sortDirection, searchValue, testCasesType, page]
    );
    const totalRowCount = stats.totalTestsCount;

    const flakyTestNamesArray = useStorageQuery(
        s => (props.projectId && props.jobId ? s.getFlakyTestNames(props.projectId, props.jobId) : []),
        [props.projectId, props.jobId]
    );

    const flakyTestNamesSet = React.useMemo(() => {
        return new Set(flakyTestNamesArray);
    }, [flakyTestNamesArray]);

    async function createAndDownloadCSV(): Promise<void> {
        const data = await storage.getTestListForCsv(props.jobRunIds);
        data.unshift(["Order#", "Test Name", "Status", "Duration(ms)"]);
        const csvString = convertToCSV(data);
        downloadCSV(`${props.jobRunIds.join("-")}.csv`, csvString);
    }

    return (
        <Suspense fallback={<div className="loading">Загрузка...</div>}>
            <SuspenseFadingWrapper fading={isFading}>
                {outputModalIds && (
                    <TestOutputModal
                        testId={outputModalIds[0]}
                        jobId={outputModalIds[1]}
                        jobRunIds={props.jobRunIds}
                        onClose={() => {
                            setOutputModalIds(undefined);
                        }}
                    />
                )}
                <RowStack baseline block gap={2}>
                    <Fit>
                        <Input
                            placeholder={"Search in tests"}
                            width={500}
                            value={searchText}
                            onValueChange={setSearchText}
                            rightIcon={
                                debouncedSearchValue != searchValue ? <Spinner type={"mini"} caption={""} /> : undefined
                            }
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type={undefined}
                            currentType={testCasesType}
                            count={stats.totalTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type="Success"
                            currentType={testCasesType}
                            count={stats.successTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type="Failed"
                            currentType={testCasesType}
                            count={stats.failedTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type="Skipped"
                            currentType={testCasesType}
                            count={stats.skippedTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fill />
                    {props.linksBlock}
                    <Fit>
                        <Button
                            use="link"
                            title="Download tests list in CSV"
                            icon={<NetDownloadIcon24Regular />}
                            onClick={() => {
                                // eslint-disable-next-line @typescript-eslint/no-floating-promises
                                createAndDownloadCSV();
                            }}>
                            Download tests as csv
                        </Button>
                    </Fit>
                </RowStack>
                <div className={styles.tableContainer}>
                    <table className={styles.testList}>
                        <thead>
                            <tr className={styles.testRunsTableHeadRow}>
                                <th style={{ width: 100 }}>Status</th>
                                <th>Name</th>
                                <th style={{ width: 80 }}>
                                    <SortHeaderLink
                                        onChangeSortKey={setSortField}
                                        onChangeSortDirection={setSortDirection}
                                        sortKey={"Duration"}
                                        currentSortDirection={sortDirection}
                                        currentSortKey={sortField}>
                                        Duration
                                    </SortHeaderLink>
                                </th>
                                <th className={styles.testRunActionsHeaderCell}></th>
                            </tr>
                        </thead>
                        <tbody>
                            {testList.map((x, i) => (
                                <TestRunRow
                                    key={i.toString() + x[TestRunQueryRowNames.TestId]}
                                    testRun={x}
                                    jobRunIds={props.jobRunIds}
                                    basePrefix={basePrefix}
                                    pathToProject={props.pathToProject}
                                    onSetSearchTextImmediate={setSearchTextImmediate}
                                    onSetOutputModalIds={setOutputModalIds}
                                    flakyTestNames={flakyTestNamesSet}
                                />
                            ))}
                        </tbody>
                    </table>
                </div>
                <Paging
                    activePage={page + 1}
                    onPageChange={x => {
                        startTransition(() => {
                            setPage(x - 1);
                        });
                    }}
                    pagesCount={Math.ceil(Number(totalRowCount) / itemsPerPage)}
                />
            </SuspenseFadingWrapper>
        </Suspense>
    );
}

function convertToCSV(data: string[][]): string {
    return data
        .map(row =>
            row
                .map(cell => {
                    if (typeof cell === "string" && (cell.includes(",") || cell.includes('"'))) {
                        return `"${cell.replace(/"/g, '""')}"`;
                    }
                    return cell;
                })
                .join(",")
        )
        .join("\n");
}

function downloadCSV(filename: string, csvData: string): void {
    const blob = new Blob([csvData], { type: "text/csv;charset=utf-8;" });

    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();

    URL.revokeObjectURL(link.href);
}
