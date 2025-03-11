import { ShareNetworkIcon } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";

import { Link, useParams } from "react-router-dom";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { ColorByState } from "../Components/Cells";
import { JonRunIcon } from "../Components/Icons";
import { getLinkToPipeline } from "../Domain/Navigation";
import { GroupBreadcrumps } from "./GroupBreadcrumps";
import { TestListView } from "./TestListView";
import { useProjectContextFromUrlParams } from "./useProjectContextFromUrlParams";

export function PipelineRunTestListPage(): React.JSX.Element {
    const { rootGroup: rootProjectStructure, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const { pipelineId = "" } = useParams();
    const pipelineInfo = useStorageQuery(s => s.getPipelineInfo(pipelineId), [pipelineId]);

    return (
        <Root>
            <ColumnStack block stretch gap={2}>
                <Fit>
                    <GroupBreadcrumps branchName={pipelineInfo.branchName} nodes={groupNodes} />
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
