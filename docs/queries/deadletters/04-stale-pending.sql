-- 04-stale-pending.sql
-- Records that have been in New or UnderReview for longer than they should be.
-- Alerting candidate: if a dead letter has been ignored for more than @AlertHours
-- hours, that is itself an operational failure (something is wrong with the
-- triage process, not just the message).
--
-- Parameters:
--   @AlertHours : threshold in hours (default 4)

DECLARE @AlertHours INT = 4;
DECLARE @Threshold DATETIME2 = DATEADD(HOUR, -@AlertHours, SYSUTCDATETIME());

SELECT
    DeadLetterRecordId,
    [Status],
    FailureCategory,
    SourceEntityName,
    SourceSubscriptionName,
    DeliveryCount,
    DeadLetteredTimeUtc,
    DATEDIFF(MINUTE, DeadLetteredTimeUtc, SYSUTCDATETIME()) AS AgeMinutes,
    DeadLetterReason,
    CorrelationId,
    StatusUpdatedUtc,
    StatusUpdatedBy
FROM [dbo].[DeadLetterRecord]
WHERE [Status] IN (N'New', N'UnderReview')
  AND DeadLetteredTimeUtc < @Threshold
ORDER BY DeadLetteredTimeUtc ASC;