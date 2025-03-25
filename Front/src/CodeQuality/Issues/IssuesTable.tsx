import { Paging } from "@skbkontur/react-ui";
import { Issue } from "../types/Issue";
import React, { useState } from "react";
import { SEVERITY_VALUES } from "../types/Severity";
import { theme } from "../../Theme/ITheme";
import styled from "styled-components";

interface IssuesTableProps {
    report: Issue[];
}

interface SortState {
    field: string;
    type: "asc" | "desc";
}

const PAGE_SIZE = 20;

export function IssuesTable({ report }: IssuesTableProps) {
    const [sort, setSort] = useState<SortState | null>(null);
    const [page, setPage] = useState(1);

    const paged = sortIssues(report, sort).slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <div>
            <TableContainer>
                <Table>
                    <thead>
                        <tr>
                            <th
                                onClick={() => {
                                    setSort(prev => changeSort(prev, "severity"));
                                }}>
                                Severity
                            </th>
                            <th
                                onClick={() => {
                                    setSort(prev => changeSort(prev, "category"));
                                }}>
                                Category
                            </th>
                            <th
                                onClick={() => {
                                    setSort(prev => changeSort(prev, "id"));
                                }}>
                                ID
                            </th>
                            <th
                                onClick={() => {
                                    setSort(prev => changeSort(prev, "path"));
                                }}>
                                Description
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        {paged.map((x) => (
                            <tr key={`${x.check_name ?? ""}-${x.location.path}-${x.fingerprint}`}>
                                <td>{x.severity}</td>
                                <td>
                                    {x.categories == null || x.categories.length === 0 ? "-" : x.categories.join(",")}
                                </td>
                                <td>{x.check_name}</td>
                                <td>
                                    {x.description}
                                    <br />
                                    {x.location.path}:
                                    {"lines" in x.location
                                        ? x.location.lines?.begin
                                        : x.location.positions?.begin?.line}
                                    <br /> {x.content?.body}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            </TableContainer>
            <Paging activePage={page} onPageChange={setPage} pagesCount={Math.ceil(report.length / PAGE_SIZE)} />
        </div>
    );
}

const TableContainer = styled.div`
    height: calc(100vh - 100px);
    max-height: calc(100vh - 100px);
    overflow-y: auto;
    overflow-x: auto;
    width: 100%;
    border-bottom: 1px solid ${theme.borderLineColor2};
    margin-bottom: 10px;
`;

const Table = styled.table`
    width: auto;
    min-width: 50%;
    border-collapse: collapse;

    th {
        position: sticky;
        top: 0;
        background-color: ${theme.primaryBackground};
        padding: 8px 16px;
        text-align: left;
        align: left;
        font-size: 14px;
        border-bottom: 1px solid ${theme.borderLineColor2};
        white-space: nowrap;

        &:nth-child(1),
        &:nth-child(2),
        &:nth-child(3) {
            white-space: nowrap;
        }

        &:nth-child(4) {
            min-width: 200px;
        }
    }

    td {
        padding: 8px 16px;
        text-align: left;
        align: left;
        font-size: 14px;
        line-height: 16px;

        &:nth-child(1),
        &:nth-child(2),
        &:nth-child(3) {
            white-space: nowrap;
        }
    }
`;

function changeSort(prev: null | SortState, field: string): null | SortState {
    if (prev == null) {
        return { field, type: "asc" };
    }
    if (prev.field === field) {
        return prev.type === "desc" ? null : { field, type: "desc" };
    }
    return { field, type: "asc" };
}

function sortIssues(issues: Issue[], sort: SortState | null): Issue[] {
    if (sort == null) {
        return issues;
    }
    return issues.toSorted((a, b) => {
        if (sort.field === "severity") {
            const s1 = SEVERITY_VALUES.indexOf(a.severity);
            const s2 = SEVERITY_VALUES.indexOf(b.severity);
            return sort.type === "asc" ? s1 - s2 : s2 - s1;
        }
        if (sort.field === "category") {
            const c1 = a.categories?.[0] ?? "";
            const c2 = b.categories?.[0] ?? "";
            return sort.type === "asc" ? c1.localeCompare(c2) : c2.localeCompare(c1);
        }
        if (sort.field === "id") {
            const i1 = a.check_name ?? "";
            const i2 = b.check_name ?? "";
            return sort.type === "asc" ? i1.localeCompare(i2) : i2.localeCompare(i1);
        }
        if (sort.field === "path") {
            // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
            const p1 = `${a.location.path}:${"lines" in a.location ? a.location.lines?.begin : a.location.positions?.begin?.line}`;
            // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
            const p2 = `${b.location.path}:${"lines" in b.location ? b.location.lines?.begin : b.location.positions?.begin?.line}`;
            return sort.type === "asc" ? p1.localeCompare(p2) : p2.localeCompare(p1);
        }
        return 0;
    });
}
