-- 04-events-paged.sql
-- Returns recent log events with cursor-based pagination by Id DESC.
-- This is the default "firehose" view for the FileIt UI (#17) - the live event
-- stream that updates as new events come in. Filterable by level, application,
-- source, and time range to narrow down what you are looking at.
--
-- Pagination works on Id DESC (newest first): caller passes @BeforeId from the
-- previous page, we return @Take events older than that. This is faster than
-- OFFSET/FETCH for large tables because Id is the PK.
--
-- Parameters:
--   @Take : how many events to return (default 100)
--   @BeforeId : return events with Id less than this (NULL = newest)
--   @MinLevel : filter by minimum level (NULL = all)
--               values: 'Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal'
--   @Application : filter by application (NULL = all hosts)
--   @SourceContext : filter by source context, prefix match with % allowed (NULL = all)
--   @Since : filter by CreatedOn >= @Since (NULL = no lower bound)

DECLARE @Take INT = 100;
DECLARE @BeforeId INT = NULL;
DECLARE @MinLevel NVARCHAR(20) = NULL;
DECLARE @Application NVARCHAR(100) = NULL;
DECLARE @SourceContext NVARCHAR(100) = NULL;
DECLARE @Since DATETIME2 = NULL;

-- Map Level names to a numeric rank so we can do "at least this level" filtering.
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
WHERE (@BeforeId IS NULL OR Id < @BeforeId)
  AND (@Application IS NULL OR Application = @Application)
  AND (@SourceContext IS NULL OR SourceContext LIKE @SourceContext)
  AND (@Since IS NULL OR CreatedOn >= @Since)
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
ORDER BY Id DESC;