CREATE TABLE commit_links (
    CommitSha String,
    ParentCommitSha String,
    Depth UInt16,
    CreateDate DateTime DEFAULT now()
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(created_at)
ORDER BY (CommitSha, depth, ParentCommitSha)
SETTINGS index_granularity = 8192;