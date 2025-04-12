CREATE TABLE IF NOT EXISTS TestRuns
(
    `JobId` LowCardinality(String),
    `JobRunId` String,
    `BranchName` String,
    `TestId` String,
    `State` Enum8('Success' = 1, 'Failed' = 2, 'Skipped' = 3),
    `Duration` Decimal(18, 0),
    `StartDateTime` DateTime,
    `AgentName` String,
    `AgentOSName` String,
    `JobUrl` String,
    `JUnitFailureMessage` String,
    `JUnitFailureOutput` String,
    `JUnitSystemOutput` String
    
)
ENGINE = MergeTree
PARTITION BY toMonday(StartDateTime)
ORDER BY (TestId, JobRunId, StartDateTime, BranchName, JobId)
TTL StartDateTime + toIntervalMonth(6)
SETTINGS index_granularity = 8192;

-- divider --
CREATE TABLE IF NOT EXISTS JobInfo
(
    `JobId` LowCardinality(String),
    `JobRunId` String,
    `JobUrl` String,
    `State` Enum8('Success' = 1, 'Failed' = 2, 'Canceled' = 3, 'Timeouted' = 4),
    `Duration` Decimal(18, 0),
    `StartDateTime` DateTime,
    `EndDateTime` DateTime,
    `PipelineSource` LowCardinality(String),
    `Triggered` LowCardinality(String),
    `BranchName` String,
    `CommitSha` String,
    `CommitMessage` String,
    `CommitAuthor` LowCardinality(String),
    `TotalTestsCount` UInt32,
    `SuccessTestsCount` UInt32,
    `FailedTestsCount` UInt32,
    `SkippedTestsCount` UInt32,
    `AgentName` String,
    `AgentOSName` LowCardinality(String),
    `ProjectId` String,
    `CustomStatusMessage` String,
    `PipelineId` String,
    `HasCodeQualityReport` UInt8
)
ENGINE = MergeTree
PARTITION BY toMonday(StartDateTime)
ORDER BY (JobId, JobRunId)
TTL StartDateTime + toIntervalMonth(6)
SETTINGS index_granularity = 8192;

-- divider --
CREATE MATERIALIZED VIEW IF NOT EXISTS TestRunsByRun
(
    `JobId` String,
    `JobRunId` String,
    `BranchName` String,
    `TestId` String,
    `State` Enum8('Success' = 1, 'Failed' = 2, 'Skipped' = 3),
    `Duration` Decimal(18, 0),
    `StartDateTime` DateTime,
    `AgentName` String,
    `AgentOSName` String
)
ENGINE = MergeTree
PARTITION BY toMonday(StartDateTime)
ORDER BY (JobId, JobRunId, TestId)
TTL StartDateTime + toIntervalYear(1)
SETTINGS index_granularity = 8192
AS SELECT
    JobId,
    JobRunId,
    BranchName,
    TestId,
    State,
    Duration,
    StartDateTime,
    AgentName,
    AgentOSName
FROM TestRuns;

-- divider --

CREATE TABLE IF NOT EXISTS CommitParents (
    ProjectId String,
    CommitSha String,
    ParentCommitSha String,
    Depth UInt16,
    AuthorName String,
    AuthorEmail String,
    MessagePreview String,
    CreateDate DateTime DEFAULT now()
)
ENGINE = ReplacingMergeTree(CreateDate)
PARTITION BY toYYYYMM(CreateDate)
ORDER BY (ProjectId, CommitSha, Depth, ParentCommitSha)
SETTINGS index_granularity = 8192;