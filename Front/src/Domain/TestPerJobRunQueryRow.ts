import { RunStatus } from "../Pages/RunStatus";

export type TestPerJobRunQueryRow = [string, string, string, RunStatus, number, string, string, string, string, string];

export const TestPerJobRunQueryRowNames = {
    JobId: 0,
    JobRunId: 1,
    BranchName: 2,
    State: 3,
    Duration: 4,
    StartDateTime: 5,
    JobUrl: 6,
    CustomStatusMessage: 7,
} as const;
