/*
    Table:       dbo.ComplexIdempotency
    Purpose:     Tracks idempotency keys submitted by clients on POST so that
                 retries with the same Idempotency-Key header return the
                 cached response instead of double-processing.

    Lifetime:    Rows older than 24 hours are eligible for cleanup. The
                 cleanup itself is a future concern (#10 doesn't promise it
                 yet), but the schema is designed to support a TimerTrigger
                 sweeper later.

    Related:     Issue #10 (Complex API simulation).
*/
CREATE TABLE dbo.ComplexIdempotency
(
    IdempotencyId       BIGINT IDENTITY(1,1)    NOT NULL,
    [Key]               NVARCHAR(128)           NOT NULL,
    RequestHash         CHAR(64)                NOT NULL,    -- SHA-256 hex of request body
    ResponseStatusCode  INT                     NOT NULL,
    ResponseBody        NVARCHAR(MAX)           NULL,
    ResponseLocation    NVARCHAR(2048)          NULL,
    CreatedUtc          DATETIME2               NOT NULL
        CONSTRAINT DF_ComplexIdempotency_CreatedUtc DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ComplexIdempotency
        PRIMARY KEY CLUSTERED (IdempotencyId),

    CONSTRAINT UQ_ComplexIdempotency_Key
        UNIQUE NONCLUSTERED ([Key]),

    INDEX IX_ComplexIdempotency_CreatedUtc
        NONCLUSTERED (CreatedUtc)
);
GO

EXEC sys.sp_addextendedproperty
    @name       = N'MS_Description',
    @value      = N'Idempotency-key cache for POST endpoints in the Complex module. See docs/complex-api.md.',
    @level0type = N'SCHEMA',      @level0name = N'dbo',
    @level1type = N'TABLE',       @level1name = N'ComplexIdempotency';
GO
