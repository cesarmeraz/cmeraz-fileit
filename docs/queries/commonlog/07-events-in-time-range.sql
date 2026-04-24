-- 07-events-in-time-range.sql
-- Returns all events between two timestamps, optionally filtered by application
-- and minimum level. Use when investigating an incident: "what was happening
-- around 3:00 AM last Tuesday?"
--
-- Parameters:
--   @From : start of the window (inclusive)
--   @To : end of the window (inclusive)
--   @Application : filter by application (NULL = all hosts)
--   @MinLevel : filter by minimum level (NULL = all)
--               values: 'Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal'
--   @Take : max rows (default 1000, cap to protect SSMS)

DECLARE @From DATETIME2 = DATEADD(HOUR, -2, SYSUTCDATETIME());
DECLARE @To DATETIME2 = SYSUTCDATETIME();
DECLARE @Application NVARCHAR(100) = NULL;
DECLARE @MinLevel NVARCHAR(20) = NULL;
DECLARE @Take INT = 1000;

DECLARE @MinLevelRank INT = CASE @MinLevel
    WHEN 'Verbose' THEN 0
    WHEN 'Debug' THEN 1
    WHEN 'Information' THEN 2
    WHEN 'Warning' THEN 3
    WHEN 'Error' THEN 4
    WHEN 'Fatal' THEN 5
    ELSE -1
END;

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
WHERE CreatedOn BETWEEN @From AND @To
  AND (@Application IS NULL OR Application = @Application)
  AND (
        @MinLevelRank = -1
        OR CASE Level
            WHEN 'Verbose' THEN 0
            WHEN 'Debug' THEN 1
            WHEN 'Information' THEN 2
            WHEN 'Warning' THEN 3
            WHEN 'Error' THEN 4
            WHEN 'Fatal' THEN 5
            ELSE 2
        END >= @MinLevelRank
      )
ORDER BY Id ASC;