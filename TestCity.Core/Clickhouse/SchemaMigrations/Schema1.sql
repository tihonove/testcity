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
CREATE TABLE IF NOT EXISTS `.inner-FlakyTestsWeeklyRaw`
(
    WeekStart   Date,
    ProjectId   LowCardinality(String),
    JobId       LowCardinality(String),
    TestId      LowCardinality(String),

    RunsState   AggregateFunction(count,  UInt32),
    FailState   AggregateFunction(sum,    UInt32),
    SeqState    AggregateFunction(groupArray, Tuple(DateTime, UInt8))
)
ENGINE = AggregatingMergeTree
ORDER BY (WeekStart, ProjectId, JobId, TestId)
TTL WeekStart + toIntervalMonth(12) DELETE
SETTINGS index_granularity = 8192;


-- divider --
CREATE MATERIALIZED VIEW IF NOT EXISTS FillFlakyWeeklyRaw
TO `.inner-FlakyTestsWeeklyRaw`
AS
SELECT
    toMonday(StartDateTime)                                 AS WeekStart,
    ProjectId,
    JobId,
    TestId,
    countState()                                            AS RunsState,
    sumState(toUInt32(State = 'Failed'))                    AS FailState,
    groupArrayState(tuple(StartDateTime, toUInt8(State)))   AS SeqState
FROM TestRuns
WHERE StartDateTime >= now() - INTERVAL 7 DAY
GROUP BY
    WeekStart,
    ProjectId,
    JobId,
    TestId;


-- divider --
CREATE TABLE IF NOT EXISTS `.inner-FlakyTestsWeekly`
(
    WeekStart    Date,
    ProjectId    LowCardinality(String),
    JobId        LowCardinality(String),
    TestId       LowCardinality(String),

    TotalRuns    UInt32,
    FailedRuns   UInt32,
    Flips        UInt32,
    FlipRate     Float32,

    LastEventTs  DateTime
)
ENGINE = ReplacingMergeTree(LastEventTs)
ORDER BY (WeekStart, ProjectId, JobId, TestId)
TTL WeekStart + toIntervalMonth(12) DELETE
SETTINGS index_granularity = 8192;


-- divider --
CREATE MATERIALIZED VIEW IF NOT EXISTS FlakyWeekly
TO `.inner-FlakyTestsWeekly`
AS
SELECT
    WeekStart,
    ProjectId,
    JobId,
    TestId,
    TotalRuns,
    FailedRuns,
    Flips,
    round(Flips / greatest(TotalRuns - 1, 1), 3)            AS FlipRate,
    now()                                                   AS LastEventTs
FROM
(
    SELECT
        WeekStart,
        ProjectId,
        JobId,
        TestId,

        countMerge(RunsState)                               AS TotalRuns,
        sumMerge(FailState)                                 AS FailedRuns,

        arrayMap(t -> t.2,
                 arraySort(x -> x.1, groupArrayMerge(SeqState))) AS StatusSeq,

        arrayCount(i -> StatusSeq[i] != StatusSeq[i-1],
                   arrayEnumerate(StatusSeq))               AS Flips
    FROM FillFlakyWeeklyRaw
    GROUP BY
        WeekStart,
        ProjectId,
        JobId,
        TestId
    HAVING TotalRuns > 1
);