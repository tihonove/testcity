export type TestRunQueryRow = ["Failed" | "Success" | "Skipped", string, number, string, string];

export const TestRunQueryRowNames = {
    State: 0,
    TestId: 1,
    Duration: 2,
    StartDateTime: 3,
    JobId: 4,
} as const;
