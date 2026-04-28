-- 06-failure-age-distribution.sql
-- Distribution of failure ages bucketed into Transient, Short, Medium, Long.
-- The bucketing aligns with the calculus-delta intuition we used to design
-- the schema: each band corresponds to a different likely failure mode.
--
-- Buckets:
--   Instant      : <    5s   - typically Poison or SchemaViolation (rejected on first parse)
--   Short        :    5s -  60s - normal retry exhaustion under steady load
--   Medium       :   1m -  10m - retry exhaustion with backoff under degraded conditions
--   Long         :  10m -   1h - DownstreamUnavailable or session lock issues
--   VeryLong     :   > 1h     - TTL expiration or stuck consumers
--
-- The same record breakdown by FailureCategory tells you whether the bucket
-- distribution lines up with the classifier's verdicts (it should: large skew
-- between bucket and category indicates either a classifier defect or a
-- pathological failure mode worth investigating).
--
-- Parameters:
--   @Days : how many days back to consider (default 7)

DECLARE @Days INT = 7;
DECLARE @Since DATETIME2 = DATEADD(DAY, -@Days, SYSUTCDATETIME());

WITH bucketed AS (
    SELECT
        DeadLetterRecordId,
        FailureCategory,
        DATEDIFF(SECOND, EnqueuedTimeUtc, DeadLetteredTimeUtc) AS AgeSeconds,
        CASE
            WHEN DATEDIFF(SECOND, EnqueuedTimeUtc, DeadLetteredTimeUtc) <    5 THEN N'1_Instant_lt_5s'
            WHEN DATEDIFF(SECOND, EnqueuedTimeUtc, DeadLetteredTimeUtc) <   60 THEN N'2_Short_5s_to_60s'
            WHEN DATEDIFF(SECOND, EnqueuedTimeUtc, DeadLetteredTimeUtc) <  600 THEN N'3_Medium_1m_to_10m'
            WHEN DATEDIFF(SECOND, EnqueuedTimeUtc, DeadLetteredTimeUtc) < 3600 THEN N'4_Long_10m_to_1h'
            ELSE                                                                   N'5_VeryLong_gt_1h'
        END AS AgeBucket
    FROM [dbo].[DeadLetterRecord]
    WHERE DeadLetteredTimeUtc >= @Since
)
SELECT
    AgeBucket,
    FailureCategory,
    COUNT(*) AS RecordCount,
    MIN(AgeSeconds) AS MinAgeSeconds,
    MAX(AgeSeconds) AS MaxAgeSeconds,
    AVG(CAST(AgeSeconds AS BIGINT)) AS AvgAgeSeconds
FROM bucketed
GROUP BY AgeBucket, FailureCategory
ORDER BY AgeBucket, RecordCount DESC;