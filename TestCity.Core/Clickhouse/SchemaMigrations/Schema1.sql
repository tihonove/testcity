CREATE TABLE IF NOT EXISTS TestRuns
(
    `JobId` LowCardinality(String),
    `JobRunId` String,
    `ProjectId` LowCardinality(String),
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
    `HasCodeQualityReport` UInt8,
    `ChangesSinceLastRun` Array(
        Tuple(
            CommitSha       String,
            Depth           UInt16,
            AuthorName      String,
            AuthorEmail     String,
            MessagePreview  String
        )
    ) DEFAULT []
)
ENGINE = MergeTree
PARTITION BY toMonday(StartDateTime)
ORDER BY (JobId, JobRunId)
TTL StartDateTime + toIntervalMonth(6)
SETTINGS index_granularity = 8192;

-- divider --
CREATE TABLE IF NOT EXISTS InProgressJobInfo
(
    `JobId` LowCardinality(String),
    `JobRunId` String,
    `JobUrl` String,
    `StartDateTime` DateTime,
    `PipelineSource` LowCardinality(String),
    `Triggered` LowCardinality(String),
    `BranchName` String,
    `CommitSha` String,
    `CommitMessage` String,
    `CommitAuthor` LowCardinality(String),
    `AgentName` String,
    `AgentOSName` LowCardinality(String),
    `ProjectId` String,
    `PipelineId` String,
    `ChangesSinceLastRun` Array(
        Tuple(
            CommitSha       String,
            Depth           UInt16,
            AuthorName      String,
            AuthorEmail     String,
            MessagePreview  String
        )
    ) DEFAULT []
)
ENGINE = MergeTree
PARTITION BY toMonday(StartDateTime)
ORDER BY (JobId, JobRunId)
TTL StartDateTime + toIntervalDay(2)
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
    BranchType Enum8('Main' = 1, 'Side' = 2) DEFAULT 'Main',
    CreateDate DateTime DEFAULT now()
)
ENGINE = ReplacingMergeTree(CreateDate)
PARTITION BY toYYYYMM(CreateDate)
ORDER BY (ProjectId, CommitSha, Depth, ParentCommitSha)
SETTINGS index_granularity = 8192;

-- divider --

CREATE TABLE IF NOT EXISTS GitLabEntities
(
    `Id` Int64,
    `Type` Enum8('Group' = 1, 'Project' = 2),
    `Title` String,
    `ParentId` Nullable(Int64),
    `ParamsJson` String,
    `UpdatedAt` DateTime DEFAULT now()
)
ENGINE = ReplacingMergeTree(UpdatedAt)
ORDER BY (Id, Type)
SETTINGS index_granularity = 8192;

-- divider --
CREATE TABLE IF NOT EXISTS TestStatsDailyData
(
    `StartDate` Date,
    `ProjectId` LowCardinality(String),
    `JobId` LowCardinality(String),
    `TestId` String,
    `RunsState` AggregateFunction(count),
    `FailState` AggregateFunction(sum, UInt32),
    `EntropyState` AggregateFunction(entropy, UInt8)
)
ENGINE = AggregatingMergeTree
ORDER BY (StartDate, ProjectId, JobId, TestId)
TTL StartDate + toIntervalDay(7) DELETE
SETTINGS index_granularity = 8192;

-- divider --
CREATE MATERIALIZED VIEW IF NOT EXISTS TestStatsDaily
TO TestStatsDailyData
AS
SELECT
    toDate(StartDateTime) as StartDate,
    ProjectId,
    JobId,
    TestId,
    countState() AS RunsState,
    sumState(toUInt32(State = 'Failed')) AS FailState,
    entropyStateIf(toUInt8(State), State != 'Skipped') AS EntropyState
FROM TestRuns
GROUP BY
    StartDate,
    ProjectId,
    JobId,
    TestId;

-- divider --
CREATE VIEW IF NOT EXISTS TestDashboardWeekly
AS
SELECT
    ProjectId,
    JobId,
    TestId,
    max(StartDate) as LastRunDate,
    countMerge(RunsState)  AS RunCount,
    sumMerge(FailState)    AS FailCount,
    entropyMerge(EntropyState) as Entropy
FROM TestStatsDaily
WHERE 
    StartDate>= today() - INTERVAL 7 DAY
GROUP BY ProjectId, JobId, TestId;

