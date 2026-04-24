-- 05-errors-and-warnings.sql
-- Returns only Error, Fatal, and Warning level events from the last N hours.
-- This is the "what is going wrong right now" query for ops dashboards and alerting.
-- Feeds #17 UI health widget and could be a trigger source for notifications.
--
-- Parameters:
--   @Hours : how many hours back to look (default 24)
--   @Take : how many events to return at most (default 200)
--   @IncludeWarnings : set to 0 to filter warnings out and see only errors (default 1)

DECLARE @Hours INT = 24;
DECLARE @Take INT = 200;
DECLARE @IncludeWarnings BIT = 1;

DECLARE @Since DATETIME2 = DATEADD(HOUR, -@Hours, SYSUTCDATETIME());

SELECT TOP (@Take)
    Id,
    CreatedOn,
    Level,
    Application,
    SourceContext,
    CorrelationId,
    InvocationId,
    EventId,
    EventName,
    Message,
    Exception
FROM [dbo].[CommonLog]
WHERE CreatedOn >= @Since
  AND (
        Level IN ('Error', 'Fatal')
        OR (@IncludeWarnings = 1 AND Level = 'Warning')
      )
ORDER BY Id DESC;