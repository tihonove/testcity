export type TestRunQueryRow = [
    "Failed" | "Success" | "Skipped", // State
    string, // TestId
    number, // AvgDuration
    number, // MinDuration
    number, // MaxDuration
    string, // JobId
    string, // AllStates
    string, // StartDateTime
    number, // TotalRuns
];

export const TestRunQueryRowNames = {
    State: 0,
    TestId: 1,
    AvgDuration: 2,
    MinDuration: 3,
    MaxDuration: 4,
    JobId: 5,
    AllStates: 6,
    StartDateTime: 7,
    TotalRuns: 8,
} as const;
