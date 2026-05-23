-- 02-correlation-timeline.sql
-- Returns the complete ordered timeline of events for a single CorrelationId.
-- This is THE drill-down query for the FileIt UI (#17): user clicks a run in the
-- "Recent Activity" table, this query fetches everything that happened in that flow.
--
-- Because a flow spans multiple function invocations (e.g. WatchInbound publishes
-- to a queue, Subscriber picks it up, that's 2+ invocations), filtering by CorrelationId
-- rather than InvocationId gives the full story across host boundaries.
--
-- Parameters:
--   @CorrelationId : the correlation to trace
--
-- Returns events in chronological order, joining up related invocations, with
-- EventName (human readable) alongside SourceContext and Message.

DECLARE @CorrelationId NVARCHAR(100) = '2ce61a8e-f5d4-44a1-8511-d0102bf45ea6';

SELECT
    Id,
    CreatedOn,
    Level,
    Application,
    SourceContext,
    InvocationId,
    EventId,
    EventName,
    Message,
    Exception
FROM [dbo].[CommonLog]
WHERE CorrelationId = @CorrelationId
ORDER BY Id ASC;