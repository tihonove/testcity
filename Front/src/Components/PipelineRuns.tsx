import { ShareNetworkIcon } from "@skbkontur/icons";
import * as React from "react";
import { Link } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchCell, SelectedOnHoverTr } from "./Cells";
import { formatRelativeTime, formatTestDuration, getText, toLocalTimeFromUtc } from "../Utils";
import { createLinkToPipelineRun, getLinkToPipeline } from "../Domain/Navigation";
import { PipelineRunsNames, PipelineRunsQueryRow } from "../Domain/Storage/PipelineRunsQueryRow";
import { GroupNode, Project } from "../Domain/Storage/Projects/GroupNode";
import { BranchBox } from "./BranchBox";
import { JobLink } from "./JobLink";
import { Hint } from "@skbkontur/react-ui";
import { TimingCell } from "./TimingCell";
import { CommitChanges } from "./CommitChanges";
import styles from "./PipelineRuns.module.css";

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
                        <td className={styles.paddingCell} style={{ paddingLeft: indentLevel * 25, paddingRight: 0 }} />
                        <td className={styles.numberCell}>
                            <Link
                                to={getLinkToPipeline(
                                    projectPath.map(x => x.title),
                                    x[PipelineRunsNames.PipelineId]
                                )}>
                                #{x[PipelineRunsNames.PipelineId]}
                            </Link>
                        </td>
                        <BranchCell>
                            <BranchBox name={x[PipelineRunsNames.BranchName]} />
                        </BranchCell>
                        <td className={styles.countCell}>
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
                                    0
                                )}
                            </JobLink>
                        </td>
                        <td className={styles.changesCell}>
                            <CommitChanges
                                totalCoveredCommitCount={x[PipelineRunsNames.TotalCoveredCommitCount]}
                                coveredCommits={x[PipelineRunsNames.CoveredCommits] || []}
                            />
                        </td>
                        <TimingCell
                            startDateTime={x[PipelineRunsNames.StartDateTime]}
                            duration={x[PipelineRunsNames.Duration]}
                        />
                    </SelectedOnHoverTr>
                ))}
        </tbody>
    );
}
