import * as React from "react";
import { useApiUrl, useBasePrefix } from "../Domain/Navigation";
import { PipelineRunsNames, PipelineRunsQueryRow } from "../Domain/PipelineRunsQueryRow";
import { Button } from "@skbkontur/react-ui";
import { RunJobModal } from "./RunJobModal";
import { ManualJobRunInfo } from "../Domain/ManualJobRunInfo";

interface ManualJobsInfoProps {
    projectId: string;
    allPipelineRuns: PipelineRunsQueryRow[];
}

export function ManualJobsInfo(props: ManualJobsInfoProps) {
    const apiUrl = useApiUrl();
    const [showRunModal, setShowRunModal] = React.useState(false);
    const [loading, setLoading] = React.useState(false);
    const [jobInfos, setReport] = React.useState<undefined | ManualJobRunInfo[]>([]);
    const projectPipelines = props.allPipelineRuns.filter(x => x[PipelineRunsNames.ProjectId] == props.projectId);
    const firstPipeline = projectPipelines[0];

    const fetcher = async () => {
        // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
        if (firstPipeline == undefined) {
            return;
        }
        setLoading(true);
        try {
            const res = await fetch(
                `${apiUrl}gitlab/${props.projectId}/pipelines/${firstPipeline[PipelineRunsNames.PipelineId]}/manual-jobs`
            );

            // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
            setReport(await res.json());
        } finally {
            setLoading(false);
        }
    };

    React.useEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        fetcher();
    }, [props.projectId]);

    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (firstPipeline == undefined) {
        return <></>;
    }

    return (
        <div>
            {showRunModal && (
                <RunJobModal
                    onClose={() => {
                        setShowRunModal(false);
                    }}
                    projectId={props.projectId}
                    jobInfos={jobInfos ?? []}
                    pipelines={projectPipelines}
                />
            )}
            {(jobInfos ?? []).length >= 1 ? (
                <Button
                    size="small"
                    onClick={() => {
                        setShowRunModal(true);
                    }}>
                    Run job...
                </Button>
            ) : (
                <Button disabled size="small">
                    Run job...
                </Button>
            )}
        </div>
    );
}
