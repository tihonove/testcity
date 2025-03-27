type ManualJobRunStatus = "Manual" | "Susccess" | "Failed";

export interface ManualJobRunInfo {
    jobId: string;
    jobRunId: string;
    status: ManualJobRunStatus;
}
