import { MediaUiCirclePlayIcon16Regular, People1CheckIcon16Light, ShareNetworkIcon16Light } from "@skbkontur/icons";
import { ColumnStack, Fit, Fixed, RowStack } from "@skbkontur/react-stack-layout";
import { Button, ComboBox, Input, Modal, Select, Toast } from "@skbkontur/react-ui";
import * as React from "react";
import { useLocalStorage } from "usehooks-ts";
import { useApiUrl, useBasePrefix } from "../Domain/Navigation";
import { PipelineRunsNames, PipelineRunsQueryRow } from "../Domain/Storage/PipelineRunsQueryRow";
import { ManualJobRunInfo } from "../Domain/ManualJobRunInfo";
import { GitCommitVertical, UserRound } from "lucide-react";
import styles from "./RunJobModal.module.css";

interface RunJobModalProps {
    projectId: string;
    jobInfos: ManualJobRunInfo[];
    pipelines: PipelineRunsQueryRow[];
    onClose: () => void;
}

export function RunJobModal(props: RunJobModalProps) {
    const apiUrl = useApiUrl();
    const [running, setRunning] = React.useState(false);
    const [jobToRun, setJobToRun] = React.useState(
        props.jobInfos.find(x => x.status == "Manual")?.jobId ?? props.jobInfos[0].jobId
    );
    const [gitlabAuthToken, setGitlabAuthToken] = useLocalStorage("test-analytics-gitlab-auth-token", "");

    const [currentPipeline, setCurrentPipeline] = React.useState(props.pipelines[0]);

    const getPipelines = (query: string) => {
        return Promise.resolve(props.pipelines);
    };

    const handleRunJob = () => {
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        (async () => {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
            const manualJobsForSelectedPipeline: ManualJobRunInfo[] = await (
                await fetch(
                    `${apiUrl}gitlab/${props.projectId}/pipelines/${currentPipeline[PipelineRunsNames.PipelineId]}/manual-jobs`
                )
            ).json();
            const jobRun = manualJobsForSelectedPipeline.find(x => x.jobId == jobToRun);
            if (jobRun == undefined) return;
            setRunning(true);
            try {
                const response = await fetch(
                    `https://git.skbkontur.ru/api/v4/projects/${props.projectId}/jobs/${jobRun.jobRunId}/${jobRun.status == "Manual" ? "play" : "retry"}`,
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
                    // eslint-disable-next-line @typescript-eslint/no-deprecated
                    Toast.push(`Failed to run job. ${JSON.stringify(data)}`);
                    return;
                }
                // eslint-disable-next-line @typescript-eslint/no-deprecated
                Toast.push("Job successfully started", {
                    label: "Open at gitlab",
                    // eslint-disable-next-line @typescript-eslint/no-unsafe-argument, @typescript-eslint/no-unsafe-member-access
                    handler: () => window.open(data.web_url),
                });
            } finally {
                setRunning(false);
                props.onClose();
            }
        })();
    };

    return (
        <Modal onClose={props.onClose} width={800}>
            <Modal.Header>Run job</Modal.Header>
            <Modal.Body>
                <ColumnStack gap={4} block stretch>
                    <Fit>
                        <RowStack baseline gap={2}>
                            <Fixed width={100}>Job status:</Fixed>
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
                            <Fixed width={100}>Pipeline:</Fixed>
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
                                                <div className={styles.commonInfo}>
                                                    <UserRound size={"1em"} /> {x[PipelineRunsNames.CommitAuthor]}
                                                    {" | "}
                                                    <GitCommitVertical size={"1em"} />{" "}
                                                    {x[PipelineRunsNames.CommitSha]?.slice(0, 8)?.toLowerCase()}{" "}
                                                </div>
                                                <div className={styles.commonMessage}>
                                                    {x[PipelineRunsNames.CommitMessage]}
                                                </div>
                                            </div>
                                        </div>
                                    )}
                                    renderValue={x => (
                                        <span>
                                            #{x[PipelineRunsNames.PipelineId]}{" "}
                                            <span className={styles.valueHint}>
                                                <ShareNetworkIcon16Light />
                                                {x[PipelineRunsNames.BranchName]}
                                            </span>
                                        </span>
                                    )}
                                />
                            </Fit>
                        </RowStack>
                    </Fit>
                    <Fit>
                        <RowStack baseline gap={2} block>
                            <Fixed width={100}>Auth token:</Fixed>
                            <Fit style={{ flexShrink: 1 }}>
                                <Input
                                    type="password"
                                    width={600}
                                    value={gitlabAuthToken}
                                    onValueChange={setGitlabAuthToken}
                                />
                                <div className={styles.hintText}>
                                    Get here:{" "}
                                    <a href="https://git.skbkontur.ru/-/user_settings/personal_access_tokens">
                                        git.skbkontur.ru/-/user_settings/personal_access_tokens
                                    </a>
                                    . Grant "api" permission.
                                </div>
                            </Fit>
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
