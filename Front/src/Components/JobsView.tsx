import { ShapeSquareIcon16Regular, ShareNetworkIcon } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { formatTestDuration, getLinkToJob, getText, toLocalTimeFromUtc } from "../Utils";
import { BranchCell, JobLinkWithResults } from "./Cells";

export type JobsQueryRow = [
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
];

interface JobsViewProps {
    projectId: string;
    allJobs: [string, string][];
    allJobRuns: JobsQueryRow[];
}

export function JobsView({ projectId: section, allJobs, allJobRuns }: JobsViewProps) {
    return (
        <JobList>
            {allJobs
                .filter(x => {
                    if (x[1]) {
                        return x[1] === section;
                    }
                    if (section === "17358") return !x[0].includes("FM · ") && !x[0].includes("Run ");
                    else if (section === "19371") return x[0].includes("FM · ");
                    else if (section === "Utilities") return x[0].includes("Run ");
                    return true;
                })
                .map(jobId => (
                    <React.Fragment key={jobId[0] + jobId[1]}>
                        <thead>
                            <tr>
                                <JobHeader colSpan={6}>
                                    <ShapeSquareIcon16Regular />{" "}
                                    <Link
                                        className="no-underline"
                                        to={`/test-analytics/jobs/${encodeURIComponent(jobId[0])}`}>
                                        {jobId[0]}
                                    </Link>
                                </JobHeader>
                            </tr>
                        </thead>
                        <tbody>
                            {allJobRuns
                                .filter(x => x[0] === jobId[0])
                                .sort((a, b) => Number(b[1]) - Number(a[1]))
                                .map(x => (
                                    <tr key={x[1]}>
                                        <PaddingCell />
                                        <NumberCell>
                                            <Link to={x[13] || getLinkToJob(x[1], x[3])}>#{x[1]}</Link>
                                        </NumberCell>
                                        <BranchCell branch={x[2]}>
                                            <ShareNetworkIcon /> {x[2]}
                                        </BranchCell>
                                        <CountCell>
                                            <JobLinkWithResults
                                                state={x[11]}
                                                to={`/test-analytics/jobs/${encodeURIComponent(jobId[0])}/runs/${encodeURIComponent(x[1])}`}>
                                                {getText(x[5], x[8], x[9], x[10], x[11], x[12])}
                                            </JobLinkWithResults>
                                        </CountCell>
                                        <StartedCell>{toLocalTimeFromUtc(x[4])}</StartedCell>
                                        <DurationCell>{formatTestDuration(x[7])}</DurationCell>
                                    </tr>
                                ))}
                        </tbody>
                    </React.Fragment>
                ))}
        </JobList>
    );
}

const JobList = styled.table`
    width: 100%;
    font-size: 14px;

    td {
        text-align: left;
        padding: 6px 8px;
    }

    thead > tr > th {
        padding-top: 16px;
    }
`;

const JobHeader = styled.th`
    margin-top: 8px;
    text-align: left;

    a {
        font-weight: 600;
    }
`;

const NumberCell = styled.td`
    width: 80px;
`;

const PaddingCell = styled.td`
    padding: 0 !important;
    width: 12px;
`;

const CountCell = styled.td`
    max-width: 300px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const StartedCell = styled.td`
    max-width: 150px;
    white-space: nowrap;
`;

const DurationCell = styled.td`
    max-width: 140px;
    white-space: nowrap;
`;

