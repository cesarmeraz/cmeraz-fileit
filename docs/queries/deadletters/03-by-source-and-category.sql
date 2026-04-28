-- 03-by-source-and-category.sql
-- Operational dashboard: pending records grouped by source channel and category.
-- Tells you at a glance "where are we burning DLQ today and why".
--
-- Useful for spotting clusters: a sudden spike of DownstreamUnavailable on a
-- single SourceEntityName usually means a downstream dependency is degraded.
-- A spread of SchemaViolation across multiple sources usually means an upstream
-- contract changed.
--
-- Parameters:
--   @Hours : how many hours back to consider (default 24)

DECLARE @Hours INT = 24;
DECLARE @Since DATETIME2 = DATEADD(HOUR, -@Hours, SYSUTCDATETIME());

SELECT
    SourceEntityName,
    SourceSubscriptionName,
    FailureCategory,
    COUNT(*) AS RecordCount,
    SUM(CASE WHEN [Status] = N'New' THEN 1 ELSE 0 END) AS NewCount,
    SUM(CASE WHEN [Status] = N'UnderReview' THEN 1 ELSE 0 END) AS UnderReviewCount,
    SUM(CASE WHEN [Status] = N'PendingReplay' THEN 1 ELSE 0 END) AS PendingReplayCount,
    SUM(CASE WHEN [Status] = N'Replayed' THEN 1 ELSE 0 END) AS ReplayedCount,
    SUM(CASE WHEN [Status] = N'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount,
    SUM(CASE WHEN [Status] = N'Discarded' THEN 1 ELSE 0 END) AS DiscardedCount,
    MIN(DeadLetteredTimeUtc) AS OldestDeadLetterUtc,
    MAX(DeadLetteredTimeUtc) AS NewestDeadLetterUtc
FROM [dbo].[DeadLetterRecord]
WHERE DeadLetteredTimeUtc >= @Since
GROUP BY SourceEntityName, SourceSubscriptionName, FailureCategory
ORDER BY RecordCount DESC, SourceEntityName, FailureCategory;