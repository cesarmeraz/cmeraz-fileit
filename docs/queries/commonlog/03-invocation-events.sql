-- 03-invocation-events.sql
-- Returns the complete ordered timeline of events for a single function invocation.
-- Use this when you want to see exactly what ONE function execution did, without
-- cross-function noise. Tighter than 02-correlation-timeline.sql which spans
-- multiple invocations.
--
-- Parameters:
--   @InvocationId : the invocation to inspect

DECLARE @InvocationId NVARCHAR(100) = '5b4415dc-dfcb-44b6-8883-a9c476036d39';

SELECT
    Id,
    CreatedOn,
    Level,
    Application,
    SourceContext,
    CorrelationId,
    EventId,
    EventName,
    Message,
    Exception,
    Properties
FROM [dbo].[CommonLog]
WHERE InvocationId = @InvocationId
ORDER BY Id ASC;