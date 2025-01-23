import { Link, useParams } from "react-router-dom";
import * as React from "react";
import { Suspense, useDeferredValue, useMemo, useState } from "react";
import { Button, DropdownMenu, Input, MenuItem, Paging, Spinner, Switcher } from "@skbkontur/react-ui";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import {
    CheckAIcon24Regular,
    NetDownloadIcon24Regular,
    ShapeCircleIcon24Solid,
    ShareNetworkIcon,
    UiFilterSortADefaultIcon16Regular,
    UiFilterSortAHighToLowIcon16Regular,
    UiFilterSortALowToHighIcon16Regular,
    UiMenuDots3VIcon16Regular,
    XIcon24Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { RouterLinkAdapter } from "../Components/RouterLinkAdapter";
import { formatDuration } from "../RunStatisticsChart/DurationUtils";
import {
    formatTestDuration,
    toLocalTimeFromUtc,
    useSearchParamAsState,
    useSearchParamDebouncedAsState,
} from "../Utils";
import { RunStatus } from "../TestHistory/TestHistory";
import { ArrowARightIcon, HomeIcon, JonIcon, JonRunIcon } from "../Components/Icons";

interface TestNameProps {
    value: string;
    onSetSearchValue: (value: string) => void;
}

function TestName(props: TestNameProps): React.JSX.Element {
    const splitValue = useMemo(() => {
        const parts = props.value.split("(");
        if (parts.length > 1) {
            const lastPart = parts.slice(1);
            const dotParts = parts[0].split(/[.]/);
            const prefix = dotParts.slice(0, -2).join(".");
            const testcaseName = dotParts.slice(-2).join(".");
            return [prefix, `${testcaseName}(${lastPart.join("(")}`];
        }

        const dotParts = props.value.split(/[.]/);
        const prefix = dotParts.slice(0, -2).join(".");
        const testcaseName = dotParts.slice(-2).join(".");
        return [prefix, testcaseName];
    }, [props.value]);
    return (
        <>
            {splitValue[1]}
            <TestNamePrefix
                onClick={() => {
                    props.onSetSearchValue(splitValue[0]);
                }}>
                {splitValue[0]}
            </TestNamePrefix>
        </>
    );
}

type SortHeaderLinkProps = {
    sortKey: string;
    onChangeSortKey: (nextValue: string | undefined) => void;
    currentSortKey: string | undefined;
    currentSortDirection: string | undefined;
    onChangeSortDirection: (nextValue: string | undefined) => void;
    children: React.ReactNode;
};

function SortHeaderLink(props: SortHeaderLinkProps): React.JSX.Element {
    return (
        <SortHeaderLinkRoot
            href="#"
            onClick={() => {
                if (props.sortKey == props.currentSortKey) {
                    if (props.currentSortDirection == undefined) props.onChangeSortDirection("desc");
                    else if (props.currentSortDirection == "desc") props.onChangeSortDirection("asc");
                    else {
                        props.onChangeSortKey(undefined);
                        props.onChangeSortDirection(undefined);
                    }
                } else {
                    props.onChangeSortKey(props.sortKey);
                    props.onChangeSortDirection(props.currentSortDirection);
                }
                return false;
            }}>
            {props.children}{" "}
            {props.sortKey == props.currentSortKey ? (
                props.currentSortDirection == undefined ? (
                    <UiFilterSortADefaultIcon16Regular />
                ) : props.currentSortDirection == "asc" ? (
                    <UiFilterSortALowToHighIcon16Regular />
                ) : (
                    <UiFilterSortAHighToLowIcon16Regular />
                )
            ) : (
                <UiFilterSortADefaultIcon16Regular />
            )}
        </SortHeaderLinkRoot>
    );
}

export function JobRunPage(): React.JSX.Element {
    const { jobId = "", jobRunId = "" } = useParams();
    const [testCasesType, setTestCasesType] = useState<"All" | "Success" | "Failed" | "Skipped">("All");
    const [sortField, setSortField] = useSearchParamAsState("sort");
    const [sortDirection, setSortDirection] = useSearchParamAsState("direction", "desc");
    const [searchText, setSearchText, debouncedSearchValue = "", setSearchTextImmediate] =
        useSearchParamDebouncedAsState("filter", 500, "");
    const [page, setPage] = useState(1);
    const itemsPerPage = 100;

    const client = useClickhouseClient();

    const [
        [
            startDateTime,
            endDateTime,
            duration,
            branchName,
            totalTestsCount,
            successTestsCount,
            failedTestsCount,
            skippedTestsCount,
            triggered,
            pipelineSource,
            jobUrl,
        ],
    ] = client.useData2<[string, string, number, string, string, string, string, string, string, string, string]>(
        `
        SELECT StartDateTime, EndDateTime, Duration, BranchName, TotalTestsCount, SuccessTestsCount, FailedTestsCount, SkippedTestsCount, Triggered, PipelineSource, JobUrl
        FROM JobInfo 
        WHERE JobId = '${jobId}' and JobRunId = '${jobRunId}'
        `,
        [jobId, jobRunId]
    );

    const searchValue = useDeferredValue(debouncedSearchValue);

    const condition = React.useMemo(() => {
        let result = `JobId = '${jobId}' AND JobRunId = '${jobRunId}'`;
        if (searchValue.trim() != "") result += ` AND TestId LIKE '%${searchValue}%'`;
        if (testCasesType !== "All") result += ` AND State = '${testCasesType}'`;
        return result;
    }, [jobId, jobRunId, searchValue, testCasesType]);

    const getTestList = React.useCallback(
        () =>
            client.useData2<[RunStatus, string, number, string]>(
                `
                SELECT State, TestId, Duration, StartDateTime 
                FROM TestRunsByRun 
                WHERE ${condition} 
                ORDER BY 
                    ${
                        sortField ??
                        `CASE 
                        WHEN State = 'Failed' THEN 1
                        WHEN State = 'Success' THEN 2
                        WHEN State = 'Skipped' THEN 3
                        ELSE 4
                    END`
                    } 
                ${sortField ? (sortDirection ?? "ASC") : ""}
                LIMIT ${(itemsPerPage * (page - 1)).toString()}, ${itemsPerPage}
                `,
                [condition, sortField, sortDirection, page]
            ),
        [condition, sortField, sortDirection, page]
    );

    const getTotalTestsCount = React.useCallback(
        () => client.useData2<[string]>(`SELECT COUNT(*) FROM TestRunsByRun WHERE ${condition}`, [condition]),
        [condition]
    );

    function convertToCSV(data: any[][]): string {
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

    async function createAndDownloadCSV(): Promise<void> {
        const data = await client.query<[string, string, string, string]>(
            `SELECT rowNumberInAllBlocks() + 1, TestId, State, Duration FROM TestRunsByRun WHERE JobId = '${jobId}' AND JobRunId = '${jobRunId}'`
        );
        data.unshift(["Order#", "Test Name", "Status", "Duration(ms)"]);
        const csvString = convertToCSV(data);
        downloadCSV(`${jobId.replace(" · ", "_").replace(" ", "_")}-${jobRunId}.csv`, csvString);
    }

    return (
        <Root>
            <ColumnStack gap={2} block stretch>
                <Fit>
                    <JobHeader>
                        <Link to={`/test-analytics/jobs`}>
                            <HomeIcon size={16} /> All jobs
                        </Link>
                        <ArrowARightIcon size={16} />
                        <Link to={`/test-analytics/jobs/${encodeURIComponent(jobId)}`}>
                            <JonIcon size={16} /> {jobId}
                        </Link>
                    </JobHeader>
                </Fit>
                <Fit>
                    <JobRunHeader>
                        <JonRunIcon size={32} />
                        <StyledLink to={jobUrl}>#{jobRunId}</StyledLink>&nbsp;at {startDateTime}
                    </JobRunHeader>
                </Fit>
                <Fit>
                    <Branch>
                        <ShareNetworkIcon /> {branchName}
                    </Branch>
                </Fit>
                <Fit>
                    <table>
                        <tbody>
                            <tr style={{ height: "20px" }}>
                                <td style={{ width: "150px", fontWeight: "bold" }}>Time</td>
                                <td>
                                    {toLocalTimeFromUtc(startDateTime, "short")} —{" "}
                                    {toLocalTimeFromUtc(endDateTime, "short")} (
                                    {formatTestDuration(duration.toString())})
                                </td>
                            </tr>
                            <tr style={{ height: "20px" }}>
                                <td style={{ width: "150px", fontWeight: "bold" }}>Triggered</td>
                                <td>{triggered.replace("@skbkontur.ru", "")}</td>
                            </tr>
                            <tr style={{ height: "20px" }}>
                                <td style={{ width: "150px", fontWeight: "bold" }}>Pipeline created by</td>
                                <td>{pipelineSource}</td>
                            </tr>
                        </tbody>
                    </table>
                </Fit>
                <Fit>
                    <Link
                        to={`/test-analytics/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/treemap`}>
                        Open tree map
                    </Link>
                </Fit>
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
                    &nbsp;&nbsp;
                    <Button
                        title={`All tests ${totalTestsCount}`}
                        use={testCasesType === "All" ? "primary" : "backless"}
                        onClick={() => setTestCasesType("All")}>
                        All {totalTestsCount}
                    </Button>
                    {successTestsCount != "0" && <>&nbsp;&nbsp;<Button
                        title={`${successTestsCount} success`}
                        use={testCasesType === "Success" ? "primary" : "backless"}
                        icon={<CheckAIcon24Regular />}
                        onClick={() => setTestCasesType("Success")}>
                        {successTestsCount}
                    </Button></>}
                    {failedTestsCount != "0" && <>&nbsp;&nbsp;<Button
                        title={`${failedTestsCount} failed`}
                        use={testCasesType === "Failed" ? "primary" : "backless"}
                        icon={<XIcon24Regular />}
                        onClick={() => setTestCasesType("Failed")}>
                        {failedTestsCount}
                    </Button></>}
                    {skippedTestsCount != "0" && <>&nbsp;&nbsp;<Button
                        title={`${skippedTestsCount} skipped`}
                        use={testCasesType === "Skipped" ? "primary" : "backless"}
                        icon={<ShapeCircleIcon24Solid />}
                        onClick={() => setTestCasesType("Skipped")}>
                        {skippedTestsCount}
                    </Button></>}
                    <Button
                        style={{ float: "right" }}
                        icon={<NetDownloadIcon24Regular />}
                        onClick={() => createAndDownloadCSV()}>
                        Download tests in CSV
                    </Button>
                </Fit>
                <Fit>
                    <Suspense fallback={"Loading test list...."}>
                        <Fetcher value={getTestList}>
                            {testList => (
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
                                            <TestRunsTableRow key={x[1]}>
                                                <StatusCell status={x[0]}>{x[0]}</StatusCell>
                                                <TestNameCell>
                                                    <TestName
                                                        onSetSearchValue={x => {
                                                            setSearchTextImmediate(x);
                                                        }}
                                                        value={x[1]}
                                                    />
                                                </TestNameCell>
                                                <DurationCell>{formatDuration(x[2], x[2])}</DurationCell>
                                                <ActionsCell>
                                                    <DropdownMenu caption={<KebabButton />}>
                                                        <MenuItem
                                                            component={RouterLinkAdapter}
                                                            href={`/test-analytics/history?id=${encodeURIComponent(x[1])}&runId=${jobRunId}`}>
                                                            Show test history
                                                        </MenuItem>
                                                    </DropdownMenu>
                                                </ActionsCell>
                                            </TestRunsTableRow>
                                        ))}
                                    </tbody>
                                </TestList>
                            )}
                        </Fetcher>
                    </Suspense>
                    <Paging
                        activePage={page}
                        onPageChange={setPage}
                        pagesCount={Math.ceil(Number(getTotalTestsCount()[0][0]) / itemsPerPage)}
                    />
                </Fit>
            </ColumnStack>
        </Root>
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

const JobHeader = styled.h2``;

const JobRunHeader = styled.h1`
    display: flex;
    font-size: 32px;
    line-height: 32px;
`;

const Root = styled.main``;

const Branch = styled.span`
    display: inline-block;
    background-color: ${props => props.theme.backgroundColor1};
    border-radius: 2px;
    padding: 4px 8px;
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

const TestNamePrefix = styled.div`
    cursor: pointer;
    font-size: ${props => props.theme.smallTextSize};
    color: ${props => props.theme.mutedTextColor};

    &:hover {
        text-decoration: underline;
    }
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

const SortHeaderLinkRoot = styled.a`
    font-size: 12px;
    color: ${props => props.theme.mutedTextColor};
    white-space: nowrap;
    text-decoration: none;

    &:hover {
        text-decoration: underline;
    }
`;
