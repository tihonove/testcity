import { ShareNetworkIcon } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchCell, JobLinkWithResults, SelectedOnHoverTr } from "../Components/Cells";
import { formatTestDuration, getText, toLocalTimeFromUtc } from "../Utils";
import { createLinkToPipelineRun, getLinkToPipeline } from "./Navigation";
import { PipelineRunsNames, PipelineRunsQueryRow } from "./PipelineRunsQueryRow";
import { GroupNode, Project } from "./Storage/Storage";

interface PipelineRunsProps {
    indentLevel: number;
    project: Project;
    currentBranchName?: string;
    rootProjectStructure: GroupNode;
    allPipelineRuns: PipelineRunsQueryRow[];
}

export function PipelineRuns({
    rootProjectStructure,
    project,
    allPipelineRuns,
    currentBranchName,
    indentLevel,
}: PipelineRunsProps) {
    const projectPath = useStorageQuery(s => s.getPathToProjectById(project.id), [project.id]) ?? [];
    const hasFailedRuns = false;
    return (
        <tbody>
            {allPipelineRuns
                .filter(x => x[PipelineRunsNames.ProjectId] == project.id)
                .map(x => (
                    <SelectedOnHoverTr key={x[PipelineRunsNames.PipelineId]}>
                        <PaddingCell style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }} />
                        <NumberCell>
                            <Link
                                to={getLinkToPipeline(
                                    projectPath.map(x => x.title),
                                    x[PipelineRunsNames.PipelineId]
                                )}>
                                #{x[PipelineRunsNames.PipelineId]}
                            </Link>
                        </NumberCell>
                        <BranchCell $defaultBranch={x[PipelineRunsNames.BranchName] == "master"}>
                            <ShareNetworkIcon /> {x[PipelineRunsNames.BranchName]}
                        </BranchCell>
                        <CountCell>
                            <JobLinkWithResults
                                state={x[PipelineRunsNames.State]}
                                to={createLinkToPipelineRun(
                                    rootProjectStructure,
                                    project.id,
                                    x[PipelineRunsNames.PipelineId],
                                    currentBranchName
                                )}>
                                {getText(
                                    x[PipelineRunsNames.TotalTestsCount].toString(),
                                    x[PipelineRunsNames.SuccessTestsCount]?.toString(),
                                    x[PipelineRunsNames.SkippedTestsCount]?.toString(),
                                    x[PipelineRunsNames.FailedTestsCount]?.toString(),
                                    x[PipelineRunsNames.State],
                                    x[PipelineRunsNames.CustomStatusMessage],
                                    x[PipelineRunsNames.HasCodeQualityReport]
                                )}
                            </JobLinkWithResults>
                        </CountCell>
                        <StartedCell>{toLocalTimeFromUtc(x[PipelineRunsNames.StartDateTime])}</StartedCell>
                        <DurationCell>{formatTestDuration(x[PipelineRunsNames.Duration].toString())}</DurationCell>
                    </SelectedOnHoverTr>
                ))}
        </tbody>
    );
}

const NumberCell = styled.td`
    width: 80px;
    text-align: left;
`;

const PaddingCell = styled.td`
    width: 12px;
    text-align: left;
`;

const CountCell = styled.td`
    max-width: 300px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    text-align: left;
`;

const StartedCell = styled.td`
    max-width: 150px;
    white-space: nowrap;
    text-align: left;
`;

const DurationCell = styled.td`
    max-width: 140px;
    white-space: nowrap;
    text-align: right;
`;
