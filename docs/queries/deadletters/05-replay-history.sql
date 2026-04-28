-- 05-replay-history.sql
-- Recent replay activity: records that have moved through the replay path,
-- showing attempts, outcomes, and operator audit trail. Use to verify that
-- the replay function is actually doing work (no rows here for a healthy day
-- means either no PendingReplay backlog or the timer is stuck).
--
-- Parameters:
--   @Days : how many days back to include (default 7)
--   @Take : max rows (default 200)

DECLARE @Days INT = 7;
DECLARE @Take INT = 200;
DECLARE @Since DATETIME2 = DATEADD(DAY, -@Days, SYSUTCDATETIME());

SELECT TOP (@Take)
    DeadLetterRecordId,
    [Status],
    FailureCategory,
    SourceEntityName,
    SourceSubscriptionName,
    ReplayAttemptCount,
    LastReplayAttemptUtc,
    LastReplayMessageId,
    StatusUpdatedUtc,
    StatusUpdatedBy,
    DeadLetteredTimeUtc,
    DATEDIFF(MINUTE, DeadLetteredTimeUtc, LastReplayAttemptUtc) AS TriageToReplayMinutes,
    DATEDIFF(MINUTE, LastReplayAttemptUtc, StatusUpdatedUtc) AS LastActionLagMinutes,
    LEFT(ResolutionNotes, 300) AS ResolutionNotesSnippet
FROM [dbo].[DeadLetterRecord]
WHERE LastReplayAttemptUtc IS NOT NULL
  AND LastReplayAttemptUtc >= @Since
ORDER BY LastReplayAttemptUtc DESC;