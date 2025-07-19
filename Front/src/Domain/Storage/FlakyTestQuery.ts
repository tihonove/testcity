export type FlakyTestQueryRow = [
    string, // ProjectId
    string, // JobId
    string, // TestId
    string, // LastRunDate
    number, // RunCount
    number, // FailCount
    number, // FlipCount
    string, // UpdatedAt
    number, // FlipRate
];

export const FlakyTestQueryRowNames = {
    ProjectId: 0,
    JobId: 1,
    TestId: 2,
    LastRunDate: 3,
    RunCount: 4,
    FailCount: 5,
    FlipCount: 6,
    UpdatedAt: 7,
    FlipRate: 8,
} as const;
