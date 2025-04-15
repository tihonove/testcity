export type JobsQueryRow = [
    string, // JobId
    string, // JobRunId
    string, // BranchName
    string, // AgentName
    string, // StartDateTime
    string | number | null, // TotalTestsCount
    string, // AgentOSName
    string | number | null, // Duration
    string | number | null, // SuccessTestsCount
    string | number | null, // SkippedTestsCount
    string | number | null, // FailedTestsCount
    "Failed" | "Success" | "Cancelled" | "Running", // State
    string, // CustomStatusMessage
    string, // JobUrl
    string, // ProjectId
    number, // HasCodeQualityReport
    Array<
        | [string, string, string, string]
        | {
              AuthorEmail: string;
              AuthorName: string;
              MessagePreview: string;
              ParentCommitSha: string;
          }
    >,
    number,
];
export const JobRunNames = {
    JobId: 0,
    JobRunId: 1,
    BranchName: 2,
    AgentName: 3,
    StartDateTime: 4,
    TotalTestsCount: 5,
    AgentOSName: 6,
    Duration: 7,
    SuccessTestsCount: 8,
    SkippedTestsCount: 9,
    FailedTestsCount: 10,
    State: 11,
    CustomStatusMessage: 12,
    JobUrl: 13,
    ProjectId: 14,
    HasCodeQualityReport: 15,
    CoveredCommits: 16,
    TotalCoveredCommitCount: 17,
} as const;

export type JobRunFullInfoQueryRow = [
    string,
    string,
    string,
    string,
    string,
    string,
    number,
    string,
    number,
    number,
    number,
    number,
    string,
    string,
    string,
    string,
    string,
    string,
    number,
    Array<
        | [string, string, string, string]
        | {
              ParentCommitSha: string;
              AuthorName: string;
              AuthorEmail: string;
              MessagePreview: string;
          }
    >,
    number,
];
export const JobRunFullInfoNames = {
    JobId: 0,
    JobRunId: 1,
    BranchName: 2,
    AgentName: 3,
    StartDateTime: 4,
    EndDateTime: 5,
    TotalTestsCount: 6,
    AgentOSName: 7,
    Duration: 8,
    SuccessTestsCount: 9,
    SkippedTestsCount: 10,
    FailedTestsCount: 11,
    State: 12,
    CustomStatusMessage: 13,
    JobUrl: 14,
    ProjectId: 15,
    PipelineSource: 16,
    Triggered: 17,
    HasCodeQualityReport: 18,
    CoveredCommits: 19,
    TotalCoveredCommitCount: 20,
} as const;
