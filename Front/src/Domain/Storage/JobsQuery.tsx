export type JobsQueryRow = [
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    string,
    number,
    Array<(
        | [string, string, string, string]
        | {
              AuthorEmail: string;
              AuthorName: string;
              MessagePreview: string;
              CommitSha: string;
          }
    )>,
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
