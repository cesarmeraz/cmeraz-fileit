-- 02-record-detail-with-timeline.sql
-- Full detail view of one DeadLetterRecord plus the upstream CommonLog timeline
-- for the same CorrelationId. This is the operator's "drill into one failure"
-- query: gives the failure context AND the story of what happened upstream that
-- led to dead-lettering.
--
-- Returns two result sets:
--   1. The DeadLetterRecord row itself (all columns).
--   2. The CommonLog entries for the same CorrelationId, ordered chronologically.
--      Useful for understanding what the upstream flow tried to do before the
--      message failed five times.
--
-- Parameters:
--   @DeadLetterRecordId : the id from the inbox or from a UI click

DECLARE @DeadLetterRecordId BIGINT = 1;

-- Result set 1: the dead-letter record
SELECT *
FROM [dbo].[DeadLetterRecord]
WHERE DeadLetterRecordId = @DeadLetterRecordId;

-- Result set 2: the upstream CommonLog timeline for this correlation
DECLARE @CorrelationId NVARCHAR(128);
SELECT @CorrelationId = CorrelationId
FROM [dbo].[DeadLetterRecord]
WHERE DeadLetterRecordId = @DeadLetterRecordId;

SELECT
    Id,
    CreatedOn,
    [Level],
    Application,
    SourceContext,
    InvocationId,
    EventId,
    EventName,
    [Message],
    Exception
FROM [dbo].[CommonLog]
WHERE CorrelationId = @CorrelationId
ORDER BY Id ASC;