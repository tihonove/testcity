import { NetDownloadIcon24Regular } from "@skbkontur/icons";
import { Button, Input, Paging, Spinner } from "@skbkontur/react-ui";
import * as React from "react";

import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Suspense, useDeferredValue } from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
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

interface TestListViewProps {
    jobRunIds: string[];
    pathToProject: string[];
    successTestsCount: number;
    skippedTestsCount: number;
    failedTestsCount: number;
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
    const [pageRaw, setPage] = useSearchParamAsState("page");
    const page = React.useMemo(() => (isNaN(Number(pageRaw ?? "0")) ? 0 : Number(pageRaw ?? "0")), [pageRaw]);
    const totalRowCount = props.successTestsCount + props.failedTestsCount + props.skippedTestsCount;
    const searchValue = useDeferredValue(debouncedSearchValue);

    const [outputModalIds, setOutputModalIds] = useSearchParamsAsState(["oid", "ojobid"]);

    const testList = useStorageQuery(
        s => s.getTestList(props.jobRunIds, sortField, sortDirection, searchValue, testCasesType, 100, page),
        [props.jobRunIds, sortField, sortDirection, searchValue, testCasesType, page]
    );

    async function createAndDownloadCSV(): Promise<void> {
        const data = await storage.getTestListForCsv(props.jobRunIds);
        data.unshift(["Order#", "Test Name", "Status", "Duration(ms)"]);
        const csvString = convertToCSV(data);
        downloadCSV(`${props.jobRunIds.join("-")}.csv`, csvString);
    }

    return (
        <Suspense fallback={<div className="loading">Загрузка...</div>}>
            <SuspenseFadingWrapper $fading={isFading}>
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
                            count={props.successTestsCount + props.failedTestsCount + props.skippedTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type="Success"
                            currentType={testCasesType}
                            count={props.successTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type="Failed"
                            currentType={testCasesType}
                            count={props.failedTestsCount}
                            onClick={setTestCasesType}
                        />
                    </Fit>
                    <Fit>
                        <TestTypeFilterButton
                            type="Skipped"
                            currentType={testCasesType}
                            count={props.skippedTestsCount}
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
                <TestList>
                    <thead>
                        <TestRunsTableHeadRow>
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
                            <th style={{ width: 20 }}></th>
                        </TestRunsTableHeadRow>
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
                            />
                        ))}
                    </tbody>
                </TestList>
                <Paging
                    activePage={page + 1}
                    onPageChange={x => {
                        startTransition(() => {
                            setPage((x - 1).toString());
                        });
                    }}
                    pagesCount={Math.ceil(Number(totalRowCount) / itemsPerPage)}
                />
            </SuspenseFadingWrapper>
        </Suspense>
    );
}

const TestRunsTableHeadRow = styled.tr({
    th: {
        fontSize: "12px",
        textAlign: "left",
        padding: "4px 8px",
    },

    borderBottom: "1px solid #eee",
});

const TestList = styled.table`
    width: 100%;
    max-width: 100vw;
    table-layout: fixed;
`;

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
