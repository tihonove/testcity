import * as React from "react";
import { PipelineRunsNames, PipelineRunsQueryRow } from "./PipelineRunsQueryRow";
import { Button, ComboBox, Input, Link, Modal, Select, Toast } from "@skbkontur/react-ui";
import { ManualJobRunInfo } from "./ManualJobRunInfo";
import {
    MediaUiCirclePlayIcon16Light,
    MediaUiCirclePlayIcon16Regular,
    People1CheckIcon16Light,
    ShapeCircleSIcon16Light,
    ShareNetworkIcon,
    ShareNetworkIcon16Light,
} from "@skbkontur/icons";
import { ColumnStack, Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import styled from "styled-components";
import { useLocalStorage } from "usehooks-ts";
import { theme } from "../Theme/ITheme";
import { useBasePrefix } from "./Navigation";

interface RunJobModalProps {
    projectId: string;
    jobInfos: ManualJobRunInfo[];
    pipelines: PipelineRunsQueryRow[];
    onClose: () => void;
}

export function RunJobModal(props: RunJobModalProps) {
    const basePrefix = useBasePrefix();
    const [running, setRunning] = React.useState(false);
    const [jobToRun, setJobToRun] = React.useState(
        props.jobInfos.find(x => x.status == "Manual")?.jobId ?? props.jobInfos[0].jobId
    );
    const [gitlabAuthToken, setGitlabAuthToken] = useLocalStorage("test-analytics-gitlab-auth-token", "");

    const [currentPipeline, setCurrentPipeline] = React.useState(props.pipelines[0]);

    const getPipelines = async (query: string) => {
        return props.pipelines;
    };

    const handleRunJob = async () => {
        // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
        const manualJobsForSelectedPipeline: ManualJobRunInfo[] = await (
            await fetch(
                `/${basePrefix}/gitlab/${props.projectId}/pipelines/${currentPipeline[PipelineRunsNames.PipelineId]}/manual-jobs`
            )
        ).json();
        const jobRunId = manualJobsForSelectedPipeline.find(x => x.jobId == jobToRun)?.jobRunId;
        setRunning(true);
        try {
            console.log("Found job run id", jobRunId);
            const response = await fetch(
                `https://git.skbkontur.ru/api/v4/projects/${props.projectId}/jobs/${jobRunId}/play`,
                {
                    method: "POST",
                    headers: {
                        "PRIVATE-TOKEN": gitlabAuthToken,
                        "Content-Type": "application/json",
                    },
                }
            );
            // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-explicit-any
            const data: any = await response.json();
            if (!response.ok) {
                Toast.push(`Failed to run job. ${JSON.stringify(data)}`);
                return;
            }
            Toast.push("Job successfully started", {
                label: "Open at gitlab",
                // eslint-disable-next-line @typescript-eslint/no-unsafe-argument, @typescript-eslint/no-unsafe-member-access
                handler: () => window.open(data.web_url),
            });
        } finally {
            setRunning(false);
            props.onClose();
        }
    };

    return (
        <Modal onClose={props.onClose} width={800}>
            <Modal.Header>Run job</Modal.Header>
            <Modal.Body>
                <ColumnStack gap={4} block stretch>
                    <Fit>
                        <RowStack baseline gap={2}>
                            <Caption width={100}>Job status:</Caption>
                            <Fit>
                                <Select
                                    width={400}
                                    value={jobToRun}
                                    items={props.jobInfos.map(x => x.jobId)}
                                    onValueChange={setJobToRun}
                                />
                            </Fit>
                        </RowStack>
                    </Fit>
                    <Fit>
                        <RowStack baseline gap={2}>
                            <Caption width={100}>Pipeline:</Caption>
                            <Fit>
                                <ComboBox<PipelineRunsQueryRow>
                                    width={400}
                                    getItems={getPipelines}
                                    onValueChange={setCurrentPipeline}
                                    value={currentPipeline}
                                    valueToString={x => x[PipelineRunsNames.PipelineId]}
                                    renderItem={x => (
                                        <div>
                                            <span>
                                                #{x[PipelineRunsNames.PipelineId]}{" "}
                                                <span>
                                                    <ShareNetworkIcon16Light /> {x[PipelineRunsNames.BranchName]}
                                                </span>
                                            </span>
                                            <div>
                                                <CommonInfo>
                                                    <People1CheckIcon16Light /> {x[PipelineRunsNames.CommitAuthor]}
                                                    {" | "}
                                                    sha: {x[PipelineRunsNames.CommitSha]
                                                        ?.slice(0, 8)
                                                        ?.toLowerCase()}{" "}
                                                </CommonInfo>
                                                <CommonMessage>{x[PipelineRunsNames.CommitMessage]}</CommonMessage>
                                            </div>
                                        </div>
                                    )}
                                    renderValue={x => (
                                        <span>
                                            #{x[PipelineRunsNames.PipelineId]}{" "}
                                            <ValueHint>
                                                <ShareNetworkIcon16Light />
                                                {x[PipelineRunsNames.BranchName]}
                                            </ValueHint>
                                        </span>
                                    )}
                                />
                            </Fit>
                        </RowStack>
                    </Fit>
                    <Fit>
                        <RowStack baseline gap={2} block>
                            <Caption width={100}>Auth token:</Caption>
                            <Value>
                                <Input
                                    type="password"
                                    width={600}
                                    value={gitlabAuthToken}
                                    onValueChange={setGitlabAuthToken}
                                />
                                <HintText>
                                    Get here:{" "}
                                    <a href="https://git.skbkontur.ru/-/user_settings/personal_access_tokens">
                                        git.skbkontur.ru/-/user_settings/personal_access_tokens
                                    </a>
                                    . Grant "api" permission.
                                </HintText>
                            </Value>
                        </RowStack>
                    </Fit>
                </ColumnStack>
            </Modal.Body>
            <Modal.Footer panel>
                <RowStack baseline gap={2}>
                    <Fit>
                        <Button
                            onClick={handleRunJob}
                            disabled={running}
                            loading={running}
                            use="primary"
                            icon={<MediaUiCirclePlayIcon16Regular />}>
                            Run job
                        </Button>
                    </Fit>
                    <Fit>
                        <Button disabled={running} onClick={props.onClose}>
                            Cancel
                        </Button>
                    </Fit>
                </RowStack>
            </Modal.Footer>
        </Modal>
    );
}

const Caption = styled(Fixed)({});

const Value = styled(Fit)`
    flex-shrink: 1;
`;

const HintText = styled.div`
    margin-top: 4px;
    font-size: 12px;
    color: ${theme.mutedTextColor};

    a {
        font-size: 12px;
        color: ${theme.mutedTextColor};
    }
`;

const ValueHint = styled.span`
    color: ${theme.mutedTextColor};
`;

const CommonMessage = styled.div`
    font-size: 12px;
    line-height: 16px;
    color: ${theme.mutedTextColor};
    max-height: 32px;
    overflow-y: hidden;
`;

const CommonInfo = styled.div`
    font-size: 12px;
    line-height: 16px;
    color: ${theme.mutedTextColor};
`;
