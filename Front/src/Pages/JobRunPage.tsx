/* eslint-disable @typescript-eslint/no-explicit-any */
import {
    CheckAIcon24Regular,
    NetDownloadIcon24Regular,
    ShapeCircleIcon24Solid,
    ShareNetworkIcon,
    UiMenuDots3VIcon16Regular,
    XIcon24Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { Button, DropdownMenu, Gapped, Input, MenuItem, Paging, Spinner } from "@skbkontur/react-ui";
import * as React from "react";

import { Suspense, useDeferredValue, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { ColorByState } from "../Components/Cells";
import { ArrowARightIcon, HomeIcon, JonIcon, JonRunIcon } from "../Components/Icons";
import { RouterLinkAdapter } from "../Components/RouterLinkAdapter";
import { formatDuration } from "../RunStatisticsChart/DurationUtils";
import { RunStatus } from "../TestHistory/TestHistory";
import { useSearchParamAsState, useSearchParamDebouncedAsState } from "../Utils";
import { SortHeaderLink } from "./SortHeaderLink";
import { urlPrefix } from "./Navigation";
import { reject } from "../TypeHelpers";

interface TestNameProps {
    value: string;
    onSetSearchValue: (value: string) => void;
}

function TestName(props: TestNameProps): React.JSX.Element {
    const splitValue = useMemo(() => {
        const parts = props.value.split("(");
        if (parts.length > 1) {
            const lastPart = parts.slice(1);
            const dotParts = (parts[0] ?? "").split(/[.]/);
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
                    props.onSetSearchValue(splitValue[0] ?? "");
                }}>
                {splitValue[0]}
            </TestNamePrefix>
        </>
    );
}

interface TestTypeFilterButtonProps {
    count: string;
    type: "All" | "Success" | "Failed" | "Skipped";
    currentType: string;
    onClick: (value: "All" | "Success" | "Failed" | "Skipped") => void;
}

function TestTypeFilterButton({
    count,
    type,
    currentType,
    onClick,
    ...props
}: TestTypeFilterButtonProps): React.JSX.Element {
    if (count != "0") {
        return (
            <Button
                title={`${count} ${type.toLowerCase()} tests`}
                use={currentType === type ? "primary" : "backless"}
                icon={
                    type === "Success" ? (
                        <CheckAIcon24Regular />
                    ) : type === "Failed" ? (
                        <XIcon24Regular />
                    ) : type === "Skipped" ? (
                        <ShapeCircleIcon24Solid />
                    ) : (
                        <></>
                    )
                }
                onClick={() => {
                    onClick(type);
                }}
                {...props}>
                {type === "All" ? "All " : ""}
                {count}
            </Button>
        );
    }
    return <></>;
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

    const data = client.useData2<
        [string, string, number, string, string, string, string, string, string, string, string, string, string]
    >(
        `
        SELECT StartDateTime, EndDateTime, Duration, BranchName, TotalTestsCount, SuccessTestsCount, FailedTestsCount, SkippedTestsCount, Triggered, PipelineSource, JobUrl, CustomStatusMessage, State
        FROM JobInfo 
        WHERE JobId = '${jobId}' and JobRunId = '${jobRunId}'
        `,
        [jobId, jobRunId]
    );

    const [
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
        customStatusMessage,
        state,
    ] = data[0] ?? reject("JobRun not found");

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
                LIMIT ${(itemsPerPage * (page - 1)).toString()}, ${itemsPerPage.toString()}
                `,
                [condition, sortField, sortDirection, page]
            ),
        [condition, sortField, sortDirection, page]
    );

    const totalRowCountRow = client.useData2<[string]>(`SELECT COUNT(*) FROM TestRunsByRun WHERE ${condition}`, [
        condition,
    ]);
    const totalRowCount = totalRowCountRow[0] ?? reject("Total row count not found");

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
            `SELECT rowNumberInAllBlocks() + 1, TestId, State, Duration FROM TestRunsByRun WHERE JobId = '${jobId}' AND JobRunId = '${jobRunId}'`
        );
        data.unshift(["Order#", "Test Name", "Status", "Duration(ms)"]);
        const csvString = convertToCSV(data);
        downloadCSV(`${jobId.replace(" · ", "_").replace(" ", "_")}-${jobRunId}.csv`, csvString);
    }

    return (
        <Root>
            <ColumnStack gap={2} block stretch>
                <JobBreadcrumbs>
                    <Link to={urlPrefix}>
                        <HomeIcon size={16} /> All jobs
                    </Link>
                    {/* <ArrowARightIcon size={16} />
                    <Link to={`/test-analytics/jobs/${encodeURIComponent(jobId)}`}>
                        <JonIcon size={16} /> {jobId}
                    </Link> */}
                </JobBreadcrumbs>
                <ColorByState state={state}>
                    <JobRunHeader>
                        <JonRunIcon size={32} />
                        <StyledLink to={jobUrl}>#{jobRunId}</StyledLink>&nbsp;at {startDateTime}
                    </JobRunHeader>
                    <StatusMessage>{customStatusMessage}</StatusMessage>
                </ColorByState>
                <Fit>
                    <Branch>
                        <ShareNetworkIcon /> {branchName}
                    </Branch>
                </Fit>
                <AdditionalJobInfo
                    startDateTime={startDateTime}
                    endDateTime={endDateTime}
                    duration={duration}
                    triggered={triggered}
                    pipelineSource={pipelineSource}
                />
                <Link
                    to={`/test-analytics/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/treemap`}>
                    Open tree map
                </Link>
                <Gapped>
                    <Input
                        placeholder={"Search in tests"}
                        width={500}
                        value={searchText}
                        onValueChange={setSearchText}
                        rightIcon={
                            debouncedSearchValue != searchValue ? <Spinner type={"mini"} caption={""} /> : undefined
                        }
                    />
                    <TestTypeFilterButton
                        type="All"
                        currentType={testCasesType}
                        count={totalTestsCount}
                        onClick={setTestCasesType}
                    />
                    <TestTypeFilterButton
                        type="Success"
                        currentType={testCasesType}
                        count={successTestsCount}
                        onClick={setTestCasesType}
                    />
                    <TestTypeFilterButton
                        type="Failed"
                        currentType={testCasesType}
                        count={failedTestsCount}
                        onClick={setTestCasesType}
                    />
                    <TestTypeFilterButton
                        type="Skipped"
                        currentType={testCasesType}
                        count={skippedTestsCount}
                        onClick={setTestCasesType}
                    />
                </Gapped>
                <Gapped>
                    <Button
                        title="Download tests list in CSV"
                        icon={<NetDownloadIcon24Regular />}
                        onClick={() => {
                            // eslint-disable-next-line @typescript-eslint/no-floating-promises
                            createAndDownloadCSV();
                        }}>
                        Download
                    </Button>
                </Gapped>
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
                        pagesCount={Math.ceil(Number(totalRowCount) / itemsPerPage)}
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
