import React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { getProjectNameById } from "../Utils";
import { JobsQueryRow } from "./JobsView";
import { ShapeSquareIcon16Solid, ShapeSquareIcon16Regular } from "@skbkontur/icons";

interface JobsMiniTreeProps {
    allGroup: string[];
    allJobs: [string, string][];
    allJobRuns: JobsQueryRow[];
}

export function JobsMiniTree({ allGroup, allJobs, allJobRuns }: JobsMiniTreeProps) {
    return (
        <JobsMap>{
            allGroup.map(section => (
                <React.Fragment key={section}>
                    <Link className="no-underline" to={`/test-analytics/projects/${encodeURIComponent(section)}`}>
                        <JobMapLevel1>{getProjectNameById(section)}</JobMapLevel1>
                    </Link>
                    {allJobs
                        .filter(x => (x[1] ? x[1] === section : true))
                        .map(j => (
                            <JobMapLevel2 title={j[0]} key={section + j[0]}>
                                {allJobRuns.filter(x => x[0] === j[0] && x[2] === "master")?.[0]?.[11] === "Failed"
                                    ? <ShapeSquareIcon16Solid style={{ color: "red" }} />
                                    : <ShapeSquareIcon16Regular />}
                                <Link
                                    className="no-underline"
                                    to={`/test-analytics/jobs/${encodeURIComponent(j[0])}`}>
                                    {j[0]}
                                </Link>
                            </JobMapLevel2>
                        ))}
                </React.Fragment>
            ))
        }
        </JobsMap>)
}

const JobsMap = styled.div`
    max-width: 300px;
`;

const JobMapLevel = styled.div`
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;
const JobMapLevel1 = styled(JobMapLevel)`
    font-size: 18px;
    line-height: 20px;
    margin-top: 16px;
`;

const JobMapLevel2 = styled(JobMapLevel)`
    margin-left: 16px;
`;
