-- 01-latest-runs.sql
-- Returns the latest N flow runs across all hosts, one row per InvocationId.
-- Each row summarizes a single function invocation: when it started, what host ran it,
-- what correlation it was part of, whether it had errors, and the total event count.
--
-- This is the "Recent Activity" feed for the FileIt UI (#17).
-- Also useful for QA: "did my test run produce a flow?"
--
-- Parameters:
--   @Take : how many runs to return (default 50)
--   @BeforeId : paging cursor - return runs with Id less than this (default NULL = newest)
--               Use the MinLogId from the previous page as @BeforeId to paginate.
--
-- Excludes the internal middleware FunctionStart/FunctionEnd (EventId 2,3) since every
-- invocation has those and they would dominate results.

DECLARE @Take INT = 50;
DECLARE @BeforeId INT = NULL;

WITH runs AS (
    SELECT
        InvocationId,
        MIN(Id) AS MinLogId,
        MAX(Id) AS MaxLogId,
        MIN(CreatedOn) AS StartedAt,
        MAX(CreatedOn) AS EndedAt,
        MAX(Application) AS Application,
        MAX(CorrelationId) AS CorrelationId,
        COUNT(*) AS EventCount,
        SUM(CASE WHEN Level IN ('Error', 'Fatal') THEN 1 ELSE 0 END) AS ErrorCount,
        SUM(CASE WHEN Level = 'Warning' THEN 1 ELSE 0 END) AS WarningCount
    FROM [dbo].[CommonLog]
    WHERE InvocationId IS NOT NULL
      AND (@BeforeId IS NULL OR Id < @BeforeId)
    GROUP BY InvocationId
)
SELECT TOP (@Take)
    InvocationId,
    CorrelationId,
    Application,
    StartedAt,
    EndedAt,
    DATEDIFF(MILLISECOND, StartedAt, EndedAt) AS DurationMs,
    EventCount,
    ErrorCount,
    WarningCount,
    CASE
        WHEN ErrorCount > 0 THEN 'Error'
        WHEN WarningCount > 0 THEN 'Warning'
        ELSE 'Success'
    END AS Status,
    MinLogId,
    MaxLogId
FROM runs
ORDER BY MaxLogId DESC;