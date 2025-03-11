import { ShareNetworkIcon, UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";

import { useDeferredValue } from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { ColorByState } from "../Components/Cells";
import { JonRunIcon } from "../Components/Icons";
import { useBasePrefix } from "../Domain/Navigation";
import { reject } from "../TypeHelpers";
import { useSearchParamDebouncedAsState } from "../Utils";
import { GroupBreadcrumps } from "./GroupBreadcrumps";
import { TestListView } from "./TestListView";
import { useProjectContextFromUrlParams } from "./useProjectContextFromUrlParams";

export function JobRunTestListPage(): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const { groupNodes } = useProjectContextFromUrlParams();
    const { jobId = "", jobRunId = "" } = useParams();
    useSearchParamDebouncedAsState("filter", 500, "");

    const client = useClickhouseClient();

    const data = client.useData2<
        [string, string, number, string, string, string, string, string, string, string, string, string, string]
    >(
        `
        SELECT 
            StartDateTime, 
            EndDateTime, 
            Duration, 
            BranchName, 
            TotalTestsCount, 
            SuccessTestsCount, 
            FailedTestsCount, 
            SkippedTestsCount, 
            Triggered, 
            PipelineSource, 
            JobUrl, 
            CustomStatusMessage, 
            State
        FROM JobInfo 
        WHERE 
            JobId = '${jobId}' 
            AND JobRunId in ['${jobRunId}']
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

    return (
        <Root>
            <ColumnStack gap={2} block stretch>
                <JobBreadcrumbs>
                    <GroupBreadcrumps branchName={branchName} nodes={groupNodes} />
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
                    to={`${basePrefix}jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/treemap`}>
                    Open tree map
                </Link>
                <Fit>
                    <TestListView
                        jobRunIds={[jobRunId]}
                        successTestsCount={Number(successTestsCount)}
                        failedTestsCount={Number(failedTestsCount)}
                        skippedTestsCount={Number(skippedTestsCount)}
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
