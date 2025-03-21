import { NetDownloadIcon24Regular, UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import { Button, DropdownMenu, Gapped, Input, MenuItem, Paging, Spinner } from "@skbkontur/react-ui";
import * as React from "react";

import { Suspense, useDeferredValue } from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { RouterLinkAdapter } from "./RouterLinkAdapter";
import { SortHeaderLink } from "./SortHeaderLink";
import { createLinkToTestHistory, useBasePrefix } from "../Domain/Navigation";
import { TestRunQueryRowNames } from "../Domain/TestRunQueryRow";
import { formatDuration } from "./RunStatisticsChart/DurationUtils";
import { useFilteredValues, useSearchParamAsState, useSearchParamDebouncedAsState } from "../Utils";
import { RunStatus } from "./RunStatus";
import { TestName } from "./TestName";
import { TestTypeFilterButton } from "./TestTypeFilterButton";
import { SuspenseFadingWrapper, useDelayedTransition } from "./useDelayedTransition";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";

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
    const client = useClickhouseClient();
    const [sortDirectionRaw, setSortDirection] = useSearchParamAsState("direction", "desc");
    const sortDirection = useFilteredValues(sortDirectionRaw, ["ASC", "DESC"] as const, undefined);
    const [searchText, setSearchText, debouncedSearchValue = "", setSearchTextImmediate] =
        useSearchParamDebouncedAsState("filter", 500, "");
    const itemsPerPage = 100;
    const [pageRaw, setPage] = useSearchParamAsState("page");
    const page = React.useMemo(() => (isNaN(Number(pageRaw ?? "0")) ? 0 : Number(pageRaw ?? "0")), [pageRaw]);
    const totalRowCount = props.successTestsCount + props.failedTestsCount + props.skippedTestsCount;
    const searchValue = useDeferredValue(debouncedSearchValue);
    const testList = useStorageQuery(
        s => s.getTestList(props.jobRunIds, sortField, sortDirection, searchValue, testCasesType, 100, page),
        [props.jobRunIds, sortField, sortDirection, searchValue, testCasesType, page]
    );
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    function convertToCSV(data: any[][]): string {
        return data
            .map(row =>
                row
                    .map(cell => {
                        if (typeof cell === "string" && (cell.includes(",") || cell.includes('"'))) {
                            return `"${cell.replace(/"/g, '""')}"`;
                        }
                        // eslint-disable-next-line @typescript-eslint/no-unsafe-return
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

    async function createAndDownloadCSV(): Promise<void> {
        const data = await client.query<[string, string, string, string]>(
            `SELECT rowNumberInAllBlocks() + 1, TestId, State, Duration FROM TestRunsByRun WHERE JobRunId in ['${props.jobRunIds.map(x => "'" + x + "'").join(",")}']`
        );
        data.unshift(["Order#", "Test Name", "Status", "Duration(ms)"]);
        const csvString = convertToCSV(data);
        downloadCSV(`${props.jobRunIds.join("-")}.csv`, csvString);
    }


    return (
        <Suspense fallback={<div className="loading">Загрузка...</div>}>
            <SuspenseFadingWrapper $fading={isFading}>
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
                        {testList.map(x => (
                            <TestRunsTableRow key={x[TestRunQueryRowNames.TestId]}>
                                <StatusCell status={x[TestRunQueryRowNames.State]}>
                                    {x[TestRunQueryRowNames.State]}
                                </StatusCell>
                                <TestNameCell>
                                    <TestName
                                        onSetSearchValue={x => {
                                            setSearchTextImmediate(x);
                                        }}
                                        value={x[TestRunQueryRowNames.TestId]}
                                    />
                                </TestNameCell>
                                <DurationCell>
                                    {formatDuration(x[TestRunQueryRowNames.Duration], x[TestRunQueryRowNames.Duration])}
                                </DurationCell>
                                <ActionsCell>
                                    <DropdownMenu caption={<KebabButton />}>
                                        <MenuItem
                                            component={RouterLinkAdapter}
                                            href={createLinkToTestHistory(
                                                basePrefix,
                                                x[TestRunQueryRowNames.TestId],
                                                props.pathToProject
                                            )}>
                                            Show test history
                                        </MenuItem>
                                    </DropdownMenu>
                                </ActionsCell>
                            </TestRunsTableRow>
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

function KebabButton() {
    return (
        <KebabButtonRoot>
            <UiMenuDots3VIcon16Regular />
        </KebabButtonRoot>
    );
}

const KebabButtonRoot = styled.span`
    display: inline-block;
    padding: 0 2px 1px 2px;
    border-radius: 10px;
    cursor: pointer;

    &:hover {
        background-color: ${props => props.theme.backgroundColor1};
    }
`;

interface FetcherProps<T> {
    value: () => T;
    children: (value: T) => React.ReactNode;
}

function Fetcher<T>(props: FetcherProps<T>): React.JSX.Element {
    const value = props.value();
    return <>{props.children(value)}</>;
}

const StyledLink = styled(Link)`
    color: inherit;
    font-size: inherit;
    line-height: inherit;
    display: inherit;
`;

const JobBreadcrumbs = styled.h2``;

const JobRunHeader = styled.h1`
    display: flex;
    font-size: 32px;
    line-height: 32px;
`;

const StatusMessage = styled.h3`
    display: flex;
    line-height: 32px;
`;

const Root = styled.main``;

const Branch = styled.span`
    display: inline-block;
    background-color: ${props => props.theme.backgroundColor1};
    border-radius: 2px;
    padding: 0 8px;
`;

const Header3 = styled.h3`
    font-weight: 500;
`;

const TestRunsTableHeadRow = styled.tr({
    th: {
        fontSize: "12px",
        textAlign: "left",
        padding: "4px 8px",
    },

    borderBottom: "1px solid #eee",
});

const TestRunsTableRow = styled.tr({
    td: {
        textAlign: "left",
        padding: "6px 8px",
    },
});

const TestList = styled.table`
    width: 100%;
    max-width: 100vw;
    table-layout: fixed;
`;

const DurationCell = styled.td`
    width: 80px;
`;
const ActionsCell = styled.td`
    width: 20px;
`;

const TestNameCell = styled.td`
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
`;

const StatusCell = styled.td<{ status: RunStatus }>`
    width: 100px;
    color: ${props =>
        props.status == "Success"
            ? props.theme.successTextColor
            : props.status == "Failed"
                ? props.theme.failedTextColor
                : undefined};
`;
