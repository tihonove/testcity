import { ShareNetworkIcon } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchCell, SelectedOnHoverTr } from "./Cells";
import { formatRelativeTime, formatTestDuration, getText, toLocalTimeFromUtc } from "../Utils";
import { createLinkToPipelineRun, getLinkToPipeline } from "../Domain/Navigation";
import { PipelineRunsNames, PipelineRunsQueryRow } from "../Domain/PipelineRunsQueryRow";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { BranchBox } from "./BranchBox";
import { JobLink } from "./JobLink";
import { theme } from "../Theme/ITheme";
import { Hint } from "@skbkontur/react-ui";
import { TimingCell } from "./TimingCell";

interface PipelineRunsProps {
    indentLevel: number;
    project: Project;
    currentBranchName?: string;
    rootProjectStructure: GroupNode;
    groupNodes: (GroupNode | Project)[];
    allPipelineRuns: PipelineRunsQueryRow[];
}

export function PipelineRuns({
    rootProjectStructure,
    project,
    allPipelineRuns,
    currentBranchName,
    indentLevel,
    groupNodes,
}: PipelineRunsProps) {
    const projectPath = groupNodes;
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
                        <BranchCell>
                            <BranchBox name={x[PipelineRunsNames.BranchName]} />
                        </BranchCell>
                        <CountCell>
                            <JobLink
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
                            </JobLink>
                        </CountCell>
                        <TimingCell
                            startDateTime={x[PipelineRunsNames.StartDateTime]}
                            duration={x[PipelineRunsNames.Duration]}
                        />
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
