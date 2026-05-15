-- 06-runs-per-day-by-application.sql
-- Aggregates distinct InvocationId counts per day per application.
-- Useful for dashboards: "how active was each host over the last N days?"
-- Also surfaces error trends: the ErrorRuns column counts invocations that had
-- at least one Error or Fatal event.
--
-- Parameters:
--   @Days : how many days back to aggregate (default 14)

DECLARE @Days INT = 14;
DECLARE @Since DATETIME2 = DATEADD(DAY, -@Days, CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIME2));

WITH per_invocation AS (
    SELECT
        CAST(MIN(CreatedOn) AS DATE) AS RunDate,
        Application,
        InvocationId,
        MAX(CASE WHEN Level IN ('Error', 'Fatal') THEN 1 ELSE 0 END) AS HadError,
        MAX(CASE WHEN Level = 'Warning' THEN 1 ELSE 0 END) AS HadWarning
    FROM [dbo].[CommonLog]
    WHERE CreatedOn >= @Since
      AND InvocationId IS NOT NULL
    GROUP BY Application, InvocationId
)
SELECT
    RunDate,
    Application,
    COUNT(*) AS TotalRuns,
    SUM(HadError) AS ErrorRuns,
    SUM(HadWarning) AS WarningRuns,
    SUM(CASE WHEN HadError = 0 AND HadWarning = 0 THEN 1 ELSE 0 END) AS CleanRuns,
    CAST(100.0 * SUM(HadError) / NULLIF(COUNT(*), 0) AS DECIMAL(5, 2)) AS ErrorPct
FROM per_invocation
GROUP BY RunDate, Application
ORDER BY RunDate DESC, Application;