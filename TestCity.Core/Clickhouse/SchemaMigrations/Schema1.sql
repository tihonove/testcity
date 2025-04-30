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

-- CREATE MATERIALIZED VIEW IF NOT EXISTS JobRunChanges
-- ENGINE = ReplacingMergeTree(AggregationVersion)
-- PARTITION BY toMonday(StartDateTime)
-- ORDER BY (JobId, JobRunId)
-- TTL StartDateTime + toIntervalYear(1)
-- SETTINGS index_granularity = 8192
-- AS
-- SELECT 
--     ji.JobId as JobId,
--     ji.JobRunId as JobRunId,
--     any(ji.StartDateTime) as StartDateTime,
--     if(any(prev.MinDepth) = 0, [], groupArray((cp2.ParentCommitSha, cp2.AuthorName, cp2.AuthorEmail, cp2.MessagePreview))) AS CoveredCommits,
--     any(prev.MinDepth) as TotalCoveredCommitCount,                    
--     if(any(prev.MinDepth) = 0, 1000, any(prev.MinDepth)) * -1 as AggregationVersion
-- FROM JobInfo ji
-- LEFT JOIN (
--     SELECT
--         prevji.ProjectId as ProjectId,
--         prevji.JobId as JobId,
--         cp.CommitSha AS CommitSha,
--         argMin(cp.ParentCommitSha, cp.Depth) AS ClosestAncestorSha,
--         min(cp.Depth) AS MinDepth
--     FROM CommitParents cp
--     INNER JOIN JobInfo prevji ON 
--         cp.ProjectId = prevji.ProjectId 
--         AND cp.ParentCommitSha = prevji.CommitSha
--         AND cp.Depth > 0
--     GROUP BY prevji.ProjectId, prevji.JobId, cp.CommitSha
-- ) AS prev ON prev.ProjectId = ji.ProjectId AND prev.JobId = ji.JobId AND prev.CommitSha = ji.CommitSha 
-- LEFT JOIN CommitParents cp2 ON cp2.ProjectId = ji.ProjectId AND cp2.CommitSha = ji.CommitSha AND prev.MinDepth != 0
-- WHERE (prev.MinDepth = 0 OR cp2.Depth < prev.MinDepth)
-- GROUP BY ji.JobId, ji.JobRunId