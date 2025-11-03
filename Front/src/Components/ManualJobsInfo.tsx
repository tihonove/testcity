import * as React from "react";
import { useApiUrl, useBasePrefix } from "../Domain/Navigation";
import { PipelineRunsNames, PipelineRunsQueryRow } from "../Domain/Storage/PipelineRunsQueryRow";
import { Button } from "@skbkontur/react-ui";
import { RunJobModal } from "./RunJobModal";
import { ManualJobRunInfo } from "../Domain/ManualJobRunInfo";
import { delay } from "../Utils/AsyncUtils";

interface ManualJobsInfoProps {
    projectId: string;
}

export function ManualJobsInfo(props: ManualJobsInfoProps) {
    // TODO Reborn this component to use allow run manual jobs
    const apiUrl = useApiUrl();
    const [showRunModal, setShowRunModal] = React.useState(false);
    const [loading, setLoading] = React.useState(false);
    const [jobInfos, setReport] = React.useState<undefined | ManualJobRunInfo[]>([]);
    const firstPipeline: PipelineRunsQueryRow | null = null;

    const fetcher = async () => {
        // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
        if (firstPipeline == undefined) {
            await delay(0);
            return;
        }
        // setLoading(true);
        // try {
        //     const res = await fetch(
        //         `${apiUrl}gitlab/${props.projectId}/pipelines/${firstPipeline[PipelineRunsNames.PipelineId]}/manual-jobs`
        //     );

        //     // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
        //     setReport(await res.json());
        // } finally {
        //     setLoading(false);
        // }
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
                    pipelines={[]}
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
