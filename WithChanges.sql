SELECT
    any(j.JobId),
    j.JobRunId,
    any(j.CommitSha),

    groupArray((cp2.ParentCommitSha, cp2.AuthorEmail, cp2.MessagePreview)) AS CoveredCommits,
    any(prev.MinDepth)

FROM JobInfo j

LEFT JOIN (

    SELECT
        prevji.JobId,
        cp.CommitSha AS CommitSha,
        argMin(cp.ParentCommitSha, cp.Depth) AS ClosestAncestorSha,
        min(cp.Depth) AS MinDepth
    FROM CommitParents cp
    INNER JOIN JobInfo prevji ON cp.ParentCommitSha = prevji.CommitSha AND cp.Depth > 0
    GROUP BY prevji.JobId, cp.CommitSha

) AS prev ON  prev.JobId = j.JobId AND prev.CommitSha = j.CommitSha 

INNER JOIN CommitParents cp2 ON cp2.CommitSha = j.CommitSha AND cp2.Depth < coalesce(prev.MinDepth, 20)

GROUP BY j.JobRunId, j.StartDateTime
ORDER BY j.StartDateTime DESC
-- LIMIT 200;