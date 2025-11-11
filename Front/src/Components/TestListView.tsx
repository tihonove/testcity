import { NetDownloadIcon24Regular } from "@skbkontur/icons";
import { Button, Input, Link, Paging, Spinner } from "@skbkontur/react-ui";
import * as React from "react";

import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Suspense, useDeferredValue } from "react";
import { useBasePrefix } from "../Domain/Navigation";
import {
    useFilteredValues,
    useSearchParamAsState,
    useSearchParamDebouncedAsState,
    useSearchParamsAsState,
} from "../Utils";
import { SortHeaderLink } from "./SortHeaderLink";
import styles from "./TestListView.module.css";
import { TestOutputModal } from "./TestOutputModal";
import { TestRunRow } from "./TestRunRow";
import { TestTypeFilterButton } from "./TestTypeFilterButton";
import { SuspenseFadingWrapper, useDelayedTransition } from "./useDelayedTransition";
import { useUrlBasedPaging } from "./useUrlBasedPaging";
import { useTestCityClient, useTestCityRequest } from "../Domain/Api/TestCityApiClient";

interface TestListViewProps {
    projectId?: string;
    jobId: string;
    jobRunId: string;
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
    const client = useTestCityClient();
    const [sortDirectionRaw, setSortDirection] = useSearchParamAsState("direction", "desc");
    const sortDirection = useFilteredValues(sortDirectionRaw, ["ASC", "DESC"] as const, undefined);
    const [searchText, setSearchText, debouncedSearchValue = "", setSearchTextImmediate] =
        useSearchParamDebouncedAsState("filter", 500, "");
    const itemsPerPage = 100;
    const [page, setPage] = useUrlBasedPaging();
    const searchValue = useDeferredValue(debouncedSearchValue);

    const [outputModalIds, setOutputModalIds] = useSearchParamsAsState(["oid", "ojobid"]);

    // const [testList, stats] = useStorageQuery(
    //     s => s.getTestListWithStat([props.jobRunId], sortField, sortDirection, searchValue, testCasesType, 100, page),
    //     [props.jobRunId, sortField, sortDirection, searchValue, testCasesType, page]
    // );
    const [testList, stats] = useTestCityRequest(
        s => {
            return Promise.all([
                s.runs.getTestList(props.pathToProject, props.jobId, props.jobRunId, {
                    sortField: sortField,
                    sortDirection: sortDirection,
                    testIdQuery: searchValue,
                    testStateFilter: testCasesType,
                    itemsPerPage: 100,
                    page: page,
                }),
                s.runs.getTestsStats(props.pathToProject, props.jobId, props.jobRunId, {
                    testIdQuery: searchValue,
                    testStateFilter: testCasesType,
                }),
            ]);
        },
        [props.jobRunId, sortField, sortDirection, searchValue, testCasesType, page]
    );
    const totalRowCount = stats.totalTestsCount;

    const flakyTestNamesArray = useTestCityRequest(
        s => (props.jobId ? s.runs.getFlakyTestsNames(props.pathToProject, props.jobId) : Promise.resolve([])),
        [props.pathToProject, props.jobId]
    );

    const flakyTestNamesSet = React.useMemo(() => {
        return new Set(flakyTestNamesArray);
    }, [flakyTestNamesArray]);

    return (
        <Suspense fallback={<div className="loading">Загрузка...</div>}>
            <SuspenseFadingWrapper fading={isFading}>
                {outputModalIds && (
                    <TestOutputModal
                        pathToProject={props.pathToProject}
                        testId={outputModalIds[0]}
                        jobId={outputModalIds[1]}
                        jobRunId={props.jobRunId}
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
                        <Link
                            title="Download tests list in CSV"
                            icon={<NetDownloadIcon24Regular />}
                            target="__blank"
                            href={client.runs.getDownloadTestsCsvUrl(props.pathToProject, props.jobId, props.jobRunId)}>
                            Download tests as csv
                        </Link>
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
                                    key={i.toString() + x.testId}
                                    testRun={x}
                                    jobRunId={props.jobRunId}
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
