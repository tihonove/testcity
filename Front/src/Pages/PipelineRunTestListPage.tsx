import { NetDownloadIcon24Regular, ShareNetworkIcon, UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import { Button, DropdownMenu, Gapped, Input, MenuItem, Paging, Spinner } from "@skbkontur/react-ui";
import * as React from "react";

import { Suspense, useDeferredValue, useMemo } from "react";
import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useClickhouseClient, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { ColorByState } from "../Components/Cells";
import { HomeIcon, JonRunIcon } from "../Components/Icons";
import { RouterLinkAdapter } from "../Components/RouterLinkAdapter";
import { SortHeaderLink } from "../Components/SortHeaderLink";
import { urlPrefix, useBasePrefix } from "../Domain/Navigation";
import { formatDuration } from "../RunStatisticsChart/DurationUtils";
import { RunStatus } from "../Components/TestHistory";
import { reject } from "../TypeHelpers";
import { useSearchParamAsState, useSearchParamDebouncedAsState } from "../Utils";
import { TestName } from "./TestName";
import { TestTypeFilterButton } from "./TestTypeFilterButton";
import { useDelayedTransition } from "./useDelayedTransition";
import { useProjectContextFromUrlParams } from "./useProjectContextFromUrlParams";
import { GroupBreadcrumps } from "./GroupBreadcrumps";
import { getLinkToPipeline } from "../Domain/Navigation";
import { TestListView } from "./TestListView";

export function PipelineRunTestListPage(): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const { rootGroup: rootProjectStructure, groupNodes, pathToGroup } = useProjectContextFromUrlParams();

    const { pipelineId = "" } = useParams();
    // const [testCasesType, setTestCasesType] = useState<"All" | "Success" | "Failed" | "Skipped">("All");
    // const [sortField, setSortField] = useSearchParamAsState("sort");
    // const [sortDirection, setSortDirection] = useSearchParamAsState("direction", "desc");
    // const [searchText, setSearchText, debouncedSearchValue = "", setSearchTextImmediate] = useSearchParamDebouncedAsState("filter", 500, "");
    // const [page, setPage] = useSearchParamAsState("page");
    // const itemsPerPage = 100;

    const pipelineInfo = useStorageQuery(s => s.getPipelineInfo(pipelineId), [pipelineId]);

    return (
        <Root>
            <ColumnStack block stretch gap={2}>
                <Fit>
                    <GroupBreadcrumps nodes={groupNodes} />
                </Fit>
                <Fit>
                    <ColorByState state={pipelineInfo.state}>
                        <JobRunHeader>
                            <JonRunIcon size={32} />
                            <StyledLink to={getLinkToPipeline(pathToGroup, pipelineInfo.pipelineId)}>
                                #{pipelineInfo.pipelineId}
                            </StyledLink>
                            &nbsp;at {pipelineInfo.startDateTime}
                        </JobRunHeader>
                        <StatusMessage>{pipelineInfo.customStatusMessage}</StatusMessage>
                    </ColorByState>
                </Fit>
                <Fit>
                    <Branch>
                        <ShareNetworkIcon /> {pipelineInfo.branchName}
                    </Branch>
                </Fit>
                <AdditionalJobInfo
                    startDateTime={pipelineInfo.startDateTime}
                    endDateTime={pipelineInfo.endDateTime}
                    duration={pipelineInfo.duration}
                    triggered={pipelineInfo.triggered}
                    pipelineSource={pipelineInfo.pipelineSource}
                />
            </ColumnStack>
            <TestListView
                jobRunIds={pipelineInfo.jobRunIds}
                successTestsCount={pipelineInfo.successTestsCount}
                skippedTestsCount={pipelineInfo.skippedTestsCount}
                failedTestsCount={pipelineInfo.failedTestsCount}
            />
        </Root>
    );
}

const Root = styled.main``;

const SuspenseFadingWrapper = styled.div<{ fading: boolean }>`
    transition: opacity 0.5s ease-in-out;
    opacity: ${props => (props.fading ? "0.3" : "1")};
`;

const JobRunHeader = styled.h1`
    display: flex;
    font-size: 32px;
    line-height: 32px;
`;

const StyledLink = styled(Link)`
    color: inherit;
    font-size: inherit;
    line-height: inherit;
    display: inherit;
`;

const StatusMessage = styled.h3`
    display: flex;
    line-height: 32px;
`;

const Branch = styled.span`
    display: inline-block;
    background-color: ${props => props.theme.backgroundColor1};
    border-radius: 2px;
    padding: 0 8px;
`;
