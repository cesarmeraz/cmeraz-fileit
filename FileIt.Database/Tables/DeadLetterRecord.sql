/*
    Table:       dbo.DeadLetterRecord
    Purpose:     Durable record of every message that is dead-lettered by Azure
                 Service Bus on any of the FileIt queues, topics, or subscriptions.
                 One row per dead-letter receive. Operator-driven replay reads
                 and updates Status on these rows.

    Companion:   dbo.CommonLog (join via CorrelationId for the full upstream story).

    Related:     Design in docs/dead-letter-strategy.md, issue #22.

    Note on CHECK constraints:
        The IN-list constraints below are written as explicit OR chains in the
        order SQL Server normalizes and stores them. Writing them as
        "CHECK (col IN ('a','b'))" is semantically identical, but sqlpackage
        diffs against the catalog's stored form (an OR chain), causing every
        publish to drop and recreate the constraint. The OR-chain form makes
        deploys idempotent. See issue #3.
*/
CREATE TABLE dbo.DeadLetterRecord
(
    DeadLetterRecordId          BIGINT IDENTITY(1,1)    NOT NULL,

    -- Identity of the failing message -------------------------------------------
    MessageId                   NVARCHAR(128)           NOT NULL,
    CorrelationId               NVARCHAR(128)           NULL,
    SessionId                   NVARCHAR(128)           NULL,

    -- Source channel ------------------------------------------------------------
    SourceEntityType            NVARCHAR(16)            NOT NULL,
    SourceEntityName            NVARCHAR(260)           NOT NULL,
    SourceSubscriptionName      NVARCHAR(260)           NULL,

    -- Service Bus failure context ----------------------------------------------
    DeadLetterReason            NVARCHAR(260)           NULL,
    DeadLetterErrorDescription  NVARCHAR(MAX)           NULL,
    DeliveryCount               INT                     NOT NULL,
    EnqueuedTimeUtc             DATETIME2               NOT NULL,
    DeadLetteredTimeUtc         DATETIME2               NOT NULL,

    -- FileIt classification -----------------------------------------------------
    FailureCategory             NVARCHAR(32)            NOT NULL,

    -- Payload (verbatim, so replay is actually possible) ------------------------
    MessageBody                 NVARCHAR(MAX)           NOT NULL,
    MessageProperties           NVARCHAR(MAX)           NULL,
    ContentType                 NVARCHAR(128)           NULL,

    -- Operator-facing lifecycle -------------------------------------------------
    [Status]                    NVARCHAR(32)            NOT NULL
        CONSTRAINT DF_DeadLetterRecord_Status
        DEFAULT ('New'),
    StatusUpdatedUtc            DATETIME2               NOT NULL
        CONSTRAINT DF_DeadLetterRecord_StatusUpdatedUtc
        DEFAULT (SYSUTCDATETIME()),
    StatusUpdatedBy             NVARCHAR(128)           NULL,

    -- Replay telemetry ----------------------------------------------------------
    ReplayAttemptCount          INT                     NOT NULL
        CONSTRAINT DF_DeadLetterRecord_ReplayAttemptCount
        DEFAULT (0),
    LastReplayAttemptUtc        DATETIME2               NULL,
    LastReplayMessageId         NVARCHAR(128)           NULL,

    -- Triage scratchpad ---------------------------------------------------------
    ResolutionNotes             NVARCHAR(MAX)           NULL,

    -- Bookkeeping ---------------------------------------------------------------
    CreatedUtc                  DATETIME2               NOT NULL
        CONSTRAINT DF_DeadLetterRecord_CreatedUtc
        DEFAULT (SYSUTCDATETIME()),

    -- Primary key ---------------------------------------------------------------
    CONSTRAINT PK_DeadLetterRecord
        PRIMARY KEY CLUSTERED (DeadLetterRecordId),

    -- Check constraints ---------------------------------------------------------
    -- (See "Note on CHECK constraints" in header. OR chains, not IN lists.)
    CONSTRAINT CK_DeadLetterRecord_SourceEntityType
        CHECK ([SourceEntityType]='Topic' OR [SourceEntityType]='Queue'),

    CONSTRAINT CK_DeadLetterRecord_SubscriptionPresence
        CHECK (
            ([SourceEntityType]=N'Queue' AND [SourceSubscriptionName] IS NULL)
            OR
            ([SourceEntityType]=N'Topic' AND [SourceSubscriptionName] IS NOT NULL)
        ),

    CONSTRAINT CK_DeadLetterRecord_FailureCategory
        CHECK ([FailureCategory]='Unknown' OR [FailureCategory]='Poison' OR [FailureCategory]='SchemaViolation' OR [FailureCategory]='DownstreamUnavailable' OR [FailureCategory]='Transient'),

    CONSTRAINT CK_DeadLetterRecord_Status
        CHECK ([Status]='Discarded' OR [Status]='Resolved' OR [Status]='Replayed' OR [Status]='PendingReplay' OR [Status]='UnderReview' OR [Status]='New'),

    CONSTRAINT CK_DeadLetterRecord_DeliveryCount_NonNegative
        CHECK (DeliveryCount >= 0),

    CONSTRAINT CK_DeadLetterRecord_ReplayAttemptCount_NonNegative
        CHECK (ReplayAttemptCount >= 0),

    -- Indexes (inline, table-scoped) --------------------------------------------
    INDEX IX_DeadLetterRecord_Status_StatusUpdatedUtc
        NONCLUSTERED ([Status], StatusUpdatedUtc DESC)
        INCLUDE (SourceEntityName, FailureCategory, CorrelationId),

    INDEX IX_DeadLetterRecord_SourceEntityName_DeadLetteredTimeUtc
        NONCLUSTERED (SourceEntityName, DeadLetteredTimeUtc DESC)
        INCLUDE ([Status], FailureCategory, CorrelationId),

    INDEX IX_DeadLetterRecord_FailureCategory_DeadLetteredTimeUtc
        NONCLUSTERED (FailureCategory, DeadLetteredTimeUtc DESC)
        INCLUDE (SourceEntityName, [Status]),

    INDEX IX_DeadLetterRecord_CorrelationId
        NONCLUSTERED (CorrelationId)
        INCLUDE (SourceEntityName, [Status], DeadLetteredTimeUtc)
        WHERE CorrelationId IS NOT NULL,

    INDEX IX_DeadLetterRecord_MessageId_Source_DeadLetteredTime
        UNIQUE NONCLUSTERED (MessageId, SourceEntityName, DeadLetteredTimeUtc)
);
GO

/*
    Extended property: table-level description.
    Surfaces in SSMS object browser and documentation generators.
*/
EXEC sys.sp_addextendedproperty
    @name       = N'MS_Description',
    @value      = N'Durable record of messages dead-lettered by Azure Service Bus on FileIt channels. One row per dead-letter receive; drives the operator-driven replay workflow. See docs/dead-letter-strategy.md.',
    @level0type = N'SCHEMA',      @level0name = N'dbo',
    @level1type = N'TABLE',       @level1name = N'DeadLetterRecord';
GO