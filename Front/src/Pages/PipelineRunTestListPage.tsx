import { ShareNetworkIcon } from "@skbkontur/icons";
import { ColumnStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";

import { Link, useParams } from "react-router-dom";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { AdditionalJobInfo } from "../Components/AdditionalJobInfo";
import { ColorByState } from "../Components/Cells";
import { JonRunIcon } from "../Components/Icons";
import { getLinkToPipeline } from "../Domain/Navigation";
import { GroupBreadcrumps } from "../Components/GroupBreadcrumps";
import { TestListView } from "../Components/TestListView";
import { useProjectContextFromUrlParams } from "../Components/useProjectContextFromUrlParams";
import { BranchBox } from "../Components/BranchBox";
import styles from "./PipelineRunTestListPage.module.css";

export function PipelineRunTestListPage(): React.JSX.Element {
    const { rootGroup: rootProjectStructure, groupNodes, pathToGroup } = useProjectContextFromUrlParams();
    const { pipelineId = "" } = useParams();
    const pipelineInfo = useStorageQuery(s => s.getPipelineInfo(pipelineId), [pipelineId]);

    return (
        <main className={styles.root}>
            <ColumnStack block stretch gap={4}>
                <Fit>
                    <GroupBreadcrumps branchName={pipelineInfo.branchName} nodes={groupNodes} />
                </Fit>
                <Fit>
                    <ColorByState state={pipelineInfo.state}>
                        <h1 className={styles.jobRunHeader}>
                            <JonRunIcon size={32} />
                            <Link
                                className={styles.styledLink}
                                to={getLinkToPipeline(pathToGroup, pipelineInfo.pipelineId)}>
                                #{pipelineInfo.pipelineId}
                            </Link>
                            &nbsp;at {pipelineInfo.startDateTime}
                        </h1>
                        <h3 className={styles.statusMessage}>{pipelineInfo.customStatusMessage}</h3>
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
                    <TestListView pathToProject={pathToGroup} jobRunIds={pipelineInfo.jobRunIds} />
                </Fit>
            </ColumnStack>
        </main>
    );
}
