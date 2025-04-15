import { ShapeSquareIcon32Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { BranchCell, NumberCell, SelectedOnHoverTr } from "../Components/Cells";
import { HomeIcon } from "../Components/Icons";
import { BranchSelect } from "../Components/BranchSelect";
import {
    formatTestDuration,
    getLinkToJob,
    getOffsetTitle,
    getText,
    toLocalTimeFromUtc,
    useSearchParamAsState,
} from "../Utils";
import { Paging } from "@skbkontur/react-ui";
import { useState } from "react";
import { createLinkToJobRun, urlPrefix } from "../Domain/Navigation";
import { JobRunNames, JobsQueryRow } from "../Domain/Storage/JobsQuery";
import { reject } from "../Utils/TypeHelpers";
import { usePopularBranchStoring } from "../Utils/PopularBranchStoring";
import { BranchBox } from "../Components/BranchBox";
import { JobLink } from "../Components/JobLink";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";

export function JobRunsPage(): React.JSX.Element {
    const { groupIdLevel1, groupIdLevel2, groupIdLevel3, jobId = "" } = useParams();
    if (groupIdLevel1 == null || groupIdLevel1 === "") {
        throw new Error(`Group is not defined`);
    }
    const { groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const rootProjectStructure = useStorageQuery(x => x.getRootProjectStructure(groupIdLevel1), [groupIdLevel1]);
    const project = useStorageQuery(x => x.getProject(pathToGroup), [pathToGroup]) ?? reject("Project not found");

    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    usePopularBranchStoring(currentBranchName);
    const [page, setPage] = useState(1);
    const itemsPerPage = 100;
    const client = useClickhouseClient();

    const condition = React.useMemo(() => {
        let result = `JobId = '${jobId}' AND ProjectId = '${project.id}'`;
        if (currentBranchName != undefined) result += ` AND BranchName = '${currentBranchName}'`;
        return result;
    }, [jobId, currentBranchName]);

    const jobRuns = client.useData2<JobsQueryRow>(
        `
        SELECT
            JobId,
            JobRunId,
            BranchName,
            AgentName,
            StartDateTime,
            TotalTestsCount,
            SuccessTestsCount,
            SkippedTestsCount,
            FailedTestsCount,
            AgentOSName,
            Duration,
            State,
            CustomStatusMessage,
            JobUrl,
            ProjectId
        FROM JobInfo
        WHERE ${condition}
        ORDER BY StartDateTime DESC
        LIMIT ${(itemsPerPage * (page - 1)).toString()}, ${itemsPerPage.toString()}
        `,
        [condition, page]
    );

    const getTotalTestsCount = React.useCallback(
        () => client.useData2<[string]>(`SELECT COUNT(*) FROM JobInfo WHERE ${condition}`, [condition]),
        [condition]
    );

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
                <RunList>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>branch</th>
                            <th></th>
                            <th>started {getOffsetTitle()}</th>
                            <th>duration</th>
                        </tr>
                    </thead>
                    <tbody>
                        {jobRuns.map(x => (
                            <SelectedOnHoverTr key={x[JobRunNames.JobRunId]}>
                                <NumberCell>
                                    <Link to={x[JobRunNames.JobUrl]}>#{x[JobRunNames.JobRunId]}</Link>
                                </NumberCell>
                                <BranchCell>
                                    <BranchBox name={x[JobRunNames.BranchName]} />
                                </BranchCell>
                                <CountCell>
                                    <JobLink
                                        state={x[JobRunNames.State]}
                                        to={createLinkToJobRun(
                                            rootProjectStructure,
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
                                </CountCell>
                                <StartedCell>{toLocalTimeFromUtc(x[JobRunNames.StartDateTime])}</StartedCell>
                                <DurationCell>
                                    {formatTestDuration(x[JobRunNames.Duration]?.toString() ?? "0")}
                                </DurationCell>
                            </SelectedOnHoverTr>
                        ))}
                    </tbody>
                </RunList>
                <Paging
                    activePage={page}
                    onPageChange={setPage}
                    pagesCount={Math.ceil(Number(getTotalTestsCount()[0][0]) / itemsPerPage)}
                />
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

const CountCell = styled.td``;

const StartedCell = styled.td`
    width: 160px;
`;

const DurationCell = styled.td`
    width: 100px;
`;
