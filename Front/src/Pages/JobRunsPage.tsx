import { ShapeSquareIcon32Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { Paging, Tooltip } from "@skbkontur/react-ui";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchBox } from "../Components/BranchBox";
import { BranchSelect } from "../Components/BranchSelect";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { JobLink } from "../Components/JobLink";
import { RotatingSpinner } from "../Components/RotatingSpinner";
import { SuspenseFadingWrapper, useDelayedTransition } from "../Components/useDelayedTransition";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { useUrlBasedPaging } from "../Components/useUrlBasedPaging";
import { createLinkToJobRun } from "../Domain/Navigation";
import { JobRunNames } from "../Domain/Storage/JobsQuery";
import { formatTestDuration, getOffsetTitle, getText, toLocalTimeFromUtc, useSearchParamAsState } from "../Utils";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { reject } from "../Utils/TypeHelpers";
import { useShowChangesFeature } from "./useShowChangesFeature";
import { theme } from "../Theme/ITheme";

export function JobRunsPage(): React.JSX.Element {
    const { jobId = "" } = useParams();
    const { groupNodes, rootGroup } = useProjectContextFromUrlParams();
    const project = groupNodes[groupNodes.length - 1] ?? reject("Project not found");
    const showChanges = useShowChangesFeature();

    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);

    const [page, setPage] = useUrlBasedPaging();
    const jobRuns = useStorageQuery(
        x => x.findAllJobsRunsPerJobId(project.id, jobId, currentBranchName, page),
        [project.id, jobId, currentBranchName, page]
    );
    const [isPending, startTransition, isFading] = useDelayedTransition();

    return (
        <ColumnStack block stretch gap={4}>
            <Fit>
                <GroupBreadcrumps branchName={currentBranchName} nodes={groupNodes} />
            </Fit>
            <Fit>
                <Header1>
                    <ShapeSquareIcon32Regular /> {jobId}
                </Header1>
            </Fit>
            <Fit>
                <BranchSelect branch={currentBranchName} onChangeBranch={setCurrentBranchName} jobId={jobId} />
            </Fit>
            <Fit>
                <SuspenseFadingWrapper $fading={isFading}>
                    <RunList>
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>branch</th>
                                <th></th>
                                {showChanges && <th>changes</th>}
                                <th>started {getOffsetTitle()}</th>
                                <th>duration</th>
                            </tr>
                        </thead>
                        <tbody>
                            {jobRuns.map(x => (
                                <SelectedOnHoverTr key={x[JobRunNames.JobRunId]}>
                                    <NumberCell>
                                        <Link to={x[13]}>#{x[1]}</Link>
                                    </NumberCell>
                                    <BranchCell>
                                        <BranchBox name={x[JobRunNames.BranchName]} />
                                    </BranchCell>
                                    <CountCell>
                                        {x[JobRunNames.State] === "Running" ? (
                                            <>
                                                <RotatingSpinner /> Running...
                                            </>
                                        ) : (
                                            <JobLink
                                                state={x[11]}
                                                to={createLinkToJobRun(
                                                    rootGroup,
                                                    project.id,
                                                    jobId,
                                                    x[JobRunNames.JobRunId],
                                                    currentBranchName
                                                )}>
                                                {getText(
                                                    x[JobRunNames.TotalTestsCount]?.toString() ?? "0",
                                                    x[JobRunNames.SuccessTestsCount]?.toString() ?? "0",
                                                    x[JobRunNames.SkippedTestsCount]?.toString() ?? "0",
                                                    x[JobRunNames.FailedTestsCount]?.toString() ?? "0",
                                                    x[JobRunNames.State],
                                                    x[JobRunNames.CustomStatusMessage],
                                                    x[JobRunNames.HasCodeQualityReport]
                                                )}
                                            </JobLink>
                                        )}
                                    </CountCell>
                                    {showChanges && (
                                        <ChangesCell>
                                            {x[JobRunNames.TotalCoveredCommitCount] > 0 ? (
                                                <Tooltip
                                                    trigger="click"
                                                    render={() => (
                                                        <ChangesList>
                                                            <tbody>
                                                                {x[JobRunNames.CoveredCommits].map((commit, index) =>
                                                                    Array.isArray(commit) ? (
                                                                        <ChangeItem key={index}>
                                                                            <Message>{commit[3]}</Message>
                                                                            <Author>
                                                                                {commit[1]} &lt;
                                                                                {commit[2]}
                                                                                &gt;
                                                                            </Author>
                                                                            <Sha>{commit[0].substring(0, 7)}</Sha>
                                                                        </ChangeItem>
                                                                    ) : (
                                                                        <ChangeItem key={index}>
                                                                            <Message>{commit.MessagePreview}</Message>
                                                                            <Author>
                                                                                {commit.AuthorName} &lt;
                                                                                {commit.AuthorEmail}
                                                                                &gt;
                                                                            </Author>
                                                                            <Sha>
                                                                                {commit.CommitSha.substring(0, 7)}
                                                                            </Sha>
                                                                        </ChangeItem>
                                                                    )
                                                                )}
                                                            </tbody>
                                                        </ChangesList>
                                                    )}>
                                                    <ChangesLink>
                                                        {x[JobRunNames.TotalCoveredCommitCount]} change
                                                        {x[JobRunNames.TotalCoveredCommitCount] !== 1 ? "s" : ""}
                                                    </ChangesLink>
                                                </Tooltip>
                                            ) : (
                                                <NoChanges>No changes</NoChanges>
                                            )}
                                        </ChangesCell>
                                    )}
                                    <StartedCell>{toLocalTimeFromUtc(x[JobRunNames.StartDateTime])}</StartedCell>
                                    <DurationCell>
                                        {formatTestDuration(x[JobRunNames.Duration]?.toString() ?? "0")}
                                    </DurationCell>
                                </SelectedOnHoverTr>
                            ))}
                        </tbody>
                    </RunList>
                    <Paging
                        activePage={page + 1}
                        onPageChange={x => {
                            startTransition(() => {
                                setPage(x - 1);
                            });
                        }}
                        pagesCount={100}
                    />
                </SuspenseFadingWrapper>
            </Fit>
        </ColumnStack>
    );
}

const HomeHeader = styled.h2``;

const Header1 = styled.h1`
    font-size: 32px;
    line-height: 40px;
    display: flex;
`;

const RunList = styled.table`
    width: 100%;
    max-width: 100vw;
    table-layout: auto;

    th {
        font-size: 12px;
        text-align: left;
        padding: 4px 8px;
    }

    td {
        text-align: left;
        padding: 6px 8px;
    }

    thead > tr {
        border-bottom: 1px solid #eee;
    }
`;

const CountCell = styled.td`
    width: 100%;
    white-space: nowrap;
`;

const StartedCell = styled.td`
    min-width: 160px;
`;

const DurationCell = styled.td`
    min-width: 100px;
`;

const ChangesCell = styled.td`
    max-width: 300px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const ChangesLink = styled.span`
    cursor: pointer;
    text-decoration: underline;
`;

const AttributesCell = styled.td`
    max-width: 80px;
    white-space: nowrap;
    text-align: left;
`;

const ChangesList = styled.table`
    padding: 10px;
    max-width: 800px;
`;

const ChangeItem = styled.tr``;

const Message = styled.td`
    font-weight: 600;
    max-width: 300px;
    font-size: 12px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const Author = styled.td`
    display: inline-block;
    font-size: 12px;
    padding-left: 5px;
    color: ${theme.mutedTextColor};
`;

const Sha = styled.td`
    font-size: 12px;
    padding-left: 5px;
    color: ${theme.mutedTextColor};
`;

const NoChanges = styled.span`
    color: ${theme.mutedTextColor};
`;
