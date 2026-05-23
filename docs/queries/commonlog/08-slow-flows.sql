-- 08-slow-flows.sql
-- Identifies function invocations whose duration (FunctionStart to FunctionEnd)
-- exceeds a threshold. This catches regressions, slow dependencies, or runaway flows.
--
-- Duration is measured from MIN(CreatedOn) to MAX(CreatedOn) within an InvocationId.
-- For multi-invocation flows (cross-host by CorrelationId), use 01-latest-runs instead
-- and filter by DurationMs.
--
-- Parameters:
--   @ThresholdMs : minimum duration in milliseconds to include (default 1000 = 1 second)
--   @Hours : how many hours back to look (default 24)
--   @Application : filter by host (NULL = all)
--   @Take : max rows returned (default 100)

DECLARE @ThresholdMs INT = 1000;
DECLARE @Hours INT = 24;
DECLARE @Application NVARCHAR(100) = NULL;
DECLARE @Take INT = 100;

DECLARE @Since DATETIME2 = DATEADD(HOUR, -@Hours, SYSUTCDATETIME());

WITH per_invocation AS (
    SELECT
        InvocationId,
        MAX(Application) AS Application,
        MAX(CorrelationId) AS CorrelationId,
        MIN(CreatedOn) AS StartedAt,
        MAX(CreatedOn) AS EndedAt,
        DATEDIFF(MILLISECOND, MIN(CreatedOn), MAX(CreatedOn)) AS DurationMs,
        COUNT(*) AS EventCount,
        SUM(CASE WHEN Level IN ('Error', 'Fatal') THEN 1 ELSE 0 END) AS ErrorCount,
        -- grab the outermost function's SourceContext so we know what slowed down
        MIN(CASE WHEN EventId = 2 THEN SourceContext END) AS EntrySourceContext
    FROM [dbo].[CommonLog]
    WHERE CreatedOn >= @Since
      AND InvocationId IS NOT NULL
      AND (@Application IS NULL OR Application = @Application)
    GROUP BY InvocationId
)
SELECT TOP (@Take)
    InvocationId,
    CorrelationId,
    Application,
    EntrySourceContext,
    StartedAt,
    EndedAt,
    DurationMs,
    EventCount,
    ErrorCount
FROM per_invocation
WHERE DurationMs >= @ThresholdMs
ORDER BY DurationMs DESC;