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
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { TestListView } from "../Components/TestListView";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { BranchBox } from "../Components/BranchBox";
import { theme } from "../Theme/ITheme";

export function PipelineRunTestListPage(): React.JSX.Element {
    const { rootGroup: rootProjectStructure, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const { pipelineId = "" } = useParams();
    const pipelineInfo = useStorageQuery(s => s.getPipelineInfo(pipelineId), [pipelineId]);

    return (
        <Root>
            <ColumnStack block stretch gap={4}>
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
                    <BranchBox name={pipelineInfo.branchName} />
                </Fit>
                <Fit>
                    <AdditionalJobInfo
                        startDateTime={pipelineInfo.startDateTime}
                        endDateTime={pipelineInfo.endDateTime}
                        duration={pipelineInfo.duration}
                        triggered={pipelineInfo.triggered}
                        pipelineSource={pipelineInfo.pipelineSource}
                    />
                </Fit>
                <Fit>
                    <TestListView
                        pathToProject={pathToGroup}
                        jobRunIds={pipelineInfo.jobRunIds}
                        successTestsCount={pipelineInfo.successTestsCount}
                        skippedTestsCount={pipelineInfo.skippedTestsCount}
                        failedTestsCount={pipelineInfo.failedTestsCount}
                    />
                </Fit>
            </ColumnStack>
        </Root>
    );
}

const Root = styled.main``;

const JobRunHeader = styled.h1`
    display: flex;
    ${theme.typography.pages.header1};
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
