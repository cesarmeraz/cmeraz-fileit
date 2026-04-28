-- 01-pending-inbox.sql
-- The dead-letter inbox: New and UnderReview records that operators must triage.
-- Sorted by DeadLetteredTimeUtc descending so the freshest failures surface first.
-- This is the default landing query for any operator paged about DLQ activity.
--
-- The "FailureAgeMinutes" column is the calculus delta we built the schema around:
-- DeadLetteredTimeUtc minus EnqueuedTimeUtc. Small values mean the message failed
-- almost immediately (likely Poison or SchemaViolation); large values mean the
-- system tried for a while before giving up (likely DownstreamUnavailable or TTL).
--
-- Parameters:
--   @Take : how many records (default 100)
--   @BeforeId : cursor pagination, return records with id < @BeforeId (NULL = newest)
--   @SourceFilter : restrict to a single SourceEntityName (NULL = all)
--   @CategoryFilter : restrict to a single FailureCategory (NULL = all)

DECLARE @Take INT = 100;
DECLARE @BeforeId BIGINT = NULL;
DECLARE @SourceFilter NVARCHAR(260) = NULL;
DECLARE @CategoryFilter NVARCHAR(32) = NULL;

SELECT TOP (@Take)
    DeadLetterRecordId,
    [Status],
    FailureCategory,
    SourceEntityType,
    SourceEntityName,
    SourceSubscriptionName,
    DeliveryCount,
    EnqueuedTimeUtc,
    DeadLetteredTimeUtc,
    DATEDIFF(MINUTE, EnqueuedTimeUtc, DeadLetteredTimeUtc) AS FailureAgeMinutes,
    DeadLetterReason,
    LEFT(DeadLetterErrorDescription, 200) AS DeadLetterErrorDescriptionSnippet,
    CorrelationId,
    MessageId,
    StatusUpdatedUtc,
    StatusUpdatedBy,
    ReplayAttemptCount,
    LastReplayAttemptUtc,
    LEFT(ResolutionNotes, 200) AS ResolutionNotesSnippet,
    CreatedUtc
FROM [dbo].[DeadLetterRecord]
WHERE [Status] IN (N'New', N'UnderReview')
  AND (@BeforeId IS NULL OR DeadLetterRecordId < @BeforeId)
  AND (@SourceFilter IS NULL OR SourceEntityName = @SourceFilter)
  AND (@CategoryFilter IS NULL OR FailureCategory = @CategoryFilter)
ORDER BY DeadLetteredTimeUtc DESC, DeadLetterRecordId DESC;