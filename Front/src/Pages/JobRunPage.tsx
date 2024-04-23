import { Link, useParams } from "react-router-dom";
import * as React from "react";
import { Dispatch, SetStateAction, Suspense, useDeferredValue, useMemo, useState } from "react";
import { DropdownMenu, Input, MenuItem, Spinner } from "@skbkontur/react-ui";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import {
    ShapeCircleMIcon16Regular,
    ShapeCircleMIcon24Regular,
    ShapeCircleMIcon32Regular,
    ShapeSquareIcon16Regular,
    ShapeSquareIcon24Regular,
    ShapeSquareIcon32Regular,
    ShareNetworkIcon,
    UiFilterSortADefaultIcon16Regular,
    UiFilterSortAHighToLowIcon16Regular,
    UiFilterSortALowToHighIcon,
    UiFilterSortALowToHighIcon16Light,
    UiFilterSortALowToHighIcon16Regular,
    UiMenuDots3VIcon16Regular,
} from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { useDebounce } from "use-debounce";
import { RouterLinkAdapter } from "../Components/RouterLinkAdapter";
import { RunStatus } from "./RunStatus";
import { formatDuration } from "../RunStatisticsChart/DurationUtils";
import { useSearchParamAsState, useSearchParamDebouncedAsState } from "../Utils";

function JonIcon(props: { size: 16 | 24 | 32; status?: RunStatus }) {
    switch (props.size) {
        case 16:
            return <ShapeSquareIcon16Regular />;
            break;
        case 24:
            return <ShapeSquareIcon24Regular />;
            break;
        case 32:
            return <ShapeSquareIcon32Regular />;
            break;
    }
}

function JonRunIcon(props: { size: 16 | 24 | 32; status?: RunStatus }) {
    switch (props.size) {
        case 16:
            return <ShapeCircleMIcon16Regular />;
            break;
        case 24:
            return <ShapeCircleMIcon24Regular />;
            break;
        case 32:
            return <ShapeCircleMIcon32Regular />;
            break;
    }
}

interface TestNameProps {
    value: string;
    onSetSearchValue: (value: string) => void;
}

function TestName(props: TestNameProps): React.JSX.Element {
    const splitValue = useMemo(() => props.value.split(/[.]/), [props.value]);
    return (
        <>
            {splitValue.slice(-2).join(".")}
            <TestNamePrefix onClick={() => props.onSetSearchValue(splitValue.slice(0, -2).join("."))}>
                {splitValue.slice(0, -2).join(".")}
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
                    console.log(111);
                    if (props.currentSortDirection == undefined || props.currentSortDirection == "asc")
                        props.onChangeSortDirection("desc");
                    else props.onChangeSortDirection("asc");
                } else {
                    props.onChangeSortKey(props.sortKey);
                }
                return false;
            }}>
            {props.children}{" "}
            {props.sortKey == props.currentSortKey ? (
                props.currentSortDirection == undefined || props.currentSortDirection == "asc" ? (
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
    const { jobId, jobRunId } = useParams();
    const [sortField, setSortField] = useSearchParamAsState("sort");
    const [sortDirection, setSortDirection] = useSearchParamAsState("direction");
    const [searchText, setSearchText, debouncedSearchValue, setSearchTextImmediate] = useSearchParamDebouncedAsState(
        "filter",
        500,
        ""
    );

    const client = useClickhouseClient();

    const [[testMaxDate, testMinDate, branchName]] = client.useData2<[string, string]>(
        `select EndDateTime, StartDateTime, BranchName from JobRunsMV where JobId = '${jobId}' and JobRunId = '${jobRunId}'`,
        [jobId, jobRunId]
    );

    const searchValue = useDeferredValue(debouncedSearchValue);

    const condition = React.useMemo(() => {
        let result = `JobId = '${jobId}' AND JobRunId = '${jobRunId}'`;
        if (searchValue?.trim() != "") result += ` AND TestId LIKE '%${searchValue}%'`;
        return result;
    }, [jobId, jobRunId, searchValue]);

    const getTestList = React.useCallback(
        () =>
            client.useData2<[RunStatus, string, number, string]>(
                `SELECT TOP 100 State, TestId, Duration, StartDateTime FROM TestRuns WHERE ${condition} ORDER BY ${sortField ?? "StartDateTime"} ${sortDirection ?? "ASC"}`,
                [condition, sortField, sortDirection]
            ),
        [condition, sortField, sortDirection]
    );

    return (
        <Root>
            <ColumnStack gap={2} block stretch>
                <Fit>
                    <JobHeader>
                        <Link to={`/test-analytics/jobs/${jobId}`}>
                            <JonIcon status={"Neutral"} size={16} /> {jobId}
                        </Link>
                    </JobHeader>
                </Fit>
                <Fit>
                    <JobRunHeader>
                        <JonRunIcon size={32} /> #{jobRunId} at {testMinDate}
                    </JobRunHeader>
                </Fit>
                <Fit>
                    <Branch>
                        <ShareNetworkIcon /> {branchName}
                    </Branch>
                </Fit>
                <Fit></Fit>
                <Fit>
                    <Header3>Tests</Header3>
                    <Link to={`/test-analytics/jobs/${jobId}/runs/${jobRunId}/treemap`}>Open tree map</Link>
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
                </Fit>
                <Fit>
                    <Suspense fallback={"Loading test list...."}>
                        <Fetcher value={getTestList}>
                            {testList => (
                                <TestList>
                                    <thead>
                                        <TestRunsTableHeadRow>
                                            <th style={{ width: 100 }}>
                                                <SortHeaderLink
                                                    onChangeSortKey={setSortField}
                                                    onChangeSortDirection={setSortDirection}
                                                    sortKey={"State"}
                                                    currentSortDirection={sortDirection}
                                                    currentSortKey={sortField}>
                                                    Status
                                                </SortHeaderLink>
                                            </th>
                                            <th>Name</th>
                                            <th style={{ width: 80 }} onClick={() => setSortField("Duration")}>
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
                                            <TestRunsTableRow>
                                                <StatusCell status={x[0]}>{x[0]}</StatusCell>
                                                <TestNameCell>
                                                    <TestName
                                                        onSetSearchValue={x => setSearchTextImmediate(x)}
                                                        value={x[1]}
                                                    />
                                                </TestNameCell>
                                                <DurationCell>{formatDuration(x[2], x[2])}</DurationCell>
                                                <ActionsCell>
                                                    <DropdownMenu caption={<KebabButton />}>
                                                        <MenuItem
                                                            component={RouterLinkAdapter}
                                                            href={`/test-analytics/history?id=${encodeURIComponent(x[1])}&branch=${branchName}`}>
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
