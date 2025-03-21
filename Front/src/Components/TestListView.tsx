import {
    ClipboardTextIcon16Regular,
    CopyIcon16Light,
    NetDownloadIcon24Regular,
    TimeClockFastIcon16Regular,
    UiMenuDots3VIcon16Regular,
} from "@skbkontur/icons";
import { Button, DropdownMenu, Gapped, Input, MenuItem, Paging, Spinner, Toast } from "@skbkontur/react-ui";
import * as React from "react";

import { Suspense, useDeferredValue } from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient, useStorage, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { RouterLinkAdapter } from "./RouterLinkAdapter";
import { SortHeaderLink } from "./SortHeaderLink";
import { createLinkToTestHistory, useBasePrefix } from "../Domain/Navigation";
import { TestRunQueryRow, TestRunQueryRowNames } from "../Domain/TestRunQueryRow";
import { formatDuration } from "./RunStatisticsChart/DurationUtils";
import {
    useFilteredValues,
    useSearchParamAsState,
    useSearchParamDebouncedAsState,
    useSearchParamsAsState,
} from "../Utils";
import { RunStatus } from "./RunStatus";
import { TestName } from "./TestName";
import { TestTypeFilterButton } from "./TestTypeFilterButton";
import { SuspenseFadingWrapper, useDelayedTransition } from "./useDelayedTransition";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { TestOutputModal } from "./TestOutputModal";
import { theme } from "../Theme/ITheme";
import { runAsyncAction } from "../TypeHelpers";
import { TestAnalyticsStorage } from "../Domain/Storage";

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

    const [outputModalIds, setOutputModalIds] = useSearchParamsAsState(["oid", "ojobid"]);

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
            `SELECT rowNumberInAllBlocks() + 1, TestId, State, Duration FROM TestRunsByRun WHERE JobRunId in [${props.jobRunIds.map(x => "'" + x + "'").join(",")}]`
        );
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

interface TestRunRowProps {
    testRun: TestRunQueryRow;
    basePrefix: string;
    pathToProject: string[];
    jobRunIds: string[];
    onSetSearchTextImmediate: (value: string) => void;
    onSetOutputModalIds: (ids: [string, string] | undefined) => void;
}

function TestRunRow({
    testRun,
    basePrefix,
    pathToProject,
    jobRunIds,
    onSetSearchTextImmediate,
    onSetOutputModalIds,
}: TestRunRowProps): React.JSX.Element {
    const [expandOutput, setExpandOutput] = React.useState(false);
    const [[failedOutput, failedMessage, systemOutput], setOutputValues] = React.useState(["", "", ""]);
    const storage = useStorage();
    React.useEffect(() => {
        if (expandOutput && !failedOutput && !failedMessage && !systemOutput)
            runAsyncAction(async () => {
                setOutputValues(
                    await storage.getFailedTestOutput(
                        testRun[TestRunQueryRowNames.JobId],
                        testRun[TestRunQueryRowNames.TestId],
                        jobRunIds
                    )
                );
            });
    }, [expandOutput]);

    const handleCopyToClipboard = React.useCallback(() => {
        runAsyncAction(async () => {
            const textToCopy = [
                testRun[TestRunQueryRowNames.TestId],
                "---",
                failedMessage,
                "---",
                failedOutput,
                "---",
                systemOutput,
            ].join("\n");
            await navigator.clipboard.writeText(textToCopy);
            Toast.push("Copied to clipboard");
        });
    }, [failedMessage, failedOutput, systemOutput, testRun[TestRunQueryRowNames.TestId]]);

    return (
        <>
            <TestRunsTableRow>
                <StatusCell status={testRun[TestRunQueryRowNames.State]}>
                    {testRun[TestRunQueryRowNames.State]}
                </StatusCell>
                <TestNameCell>
                    <TestName
                        onTestNameClick={
                            testRun[TestRunQueryRowNames.State] === "Failed"
                                ? () => {
                                      setExpandOutput(!expandOutput);
                                  }
                                : undefined
                        }
                        onSetSearchValue={x => {
                            onSetSearchTextImmediate(x);
                        }}
                        value={testRun[TestRunQueryRowNames.TestId]}
                    />
                </TestNameCell>
                <DurationCell>
                    {formatDuration(testRun[TestRunQueryRowNames.Duration], testRun[TestRunQueryRowNames.Duration])}
                </DurationCell>
                <ActionsCell>
                    <DropdownMenu caption={<KebabButton />}>
                        <MenuItem
                            icon={<TimeClockFastIcon16Regular />}
                            href={createLinkToTestHistory(
                                basePrefix,
                                testRun[TestRunQueryRowNames.TestId],
                                pathToProject
                            )}>
                            Show test history
                        </MenuItem>
                        <MenuItem
                            icon={<ClipboardTextIcon16Regular />}
                            disabled={testRun[TestRunQueryRowNames.State] !== "Failed"}
                            onClick={() => {
                                onSetOutputModalIds([
                                    testRun[TestRunQueryRowNames.TestId],
                                    testRun[TestRunQueryRowNames.JobId],
                                ]);
                            }}>
                            Show test outpout
                        </MenuItem>
                    </DropdownMenu>
                </ActionsCell>
            </TestRunsTableRow>
            <TestOutputRow $expanded={expandOutput}>
                <TestOutputCell colSpan={4}>
                    {expandOutput && (failedOutput || failedMessage || systemOutput) && (
                        <>
                            <RowStack block>
                                <Fill />
                                <Fit style={{ fontSize: "12px" }}>
                                    <Button onClick={handleCopyToClipboard} use="link" icon={<CopyIcon16Light />}>
                                        Copy to clipboard
                                    </Button>
                                </Fit>
                            </RowStack>
                            <Code>
                                {failedOutput}
                                ---
                                {failedMessage}
                                ---
                                {systemOutput}
                            </Code>
                            <a
                                href="#"
                                onClick={e => {
                                    onSetOutputModalIds([
                                        testRun[TestRunQueryRowNames.TestId],
                                        testRun[TestRunQueryRowNames.JobId],
                                    ]);
                                    e.preventDefault();
                                    return false;
                                }}>
                                Open in modal window
                            </a>
                        </>
                    )}
                </TestOutputCell>
            </TestOutputRow>
        </>
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

const TestOutputRow = styled.tr<{ $expanded?: boolean }>`
    max-height: ${props => (props.$expanded ? "none" : "0")};

    & > td {
        padding: ${props => (props.$expanded ? "6px 0 6px 24px" : "0 0 0 24px")};
    }
`;

const TestOutputCell = styled.td``;

const Code = styled.pre`
    font-size: 14px;
    line-height: 18px;
    max-height: 800px;
    margin-top: 5px;
    margin-bottom: 5px;
    // max-width: ;
    overflow: hidden;
    padding: 15px;
    border: 1px solid ${theme.borderLineColor2};
`;

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
