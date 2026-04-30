/*
    Table:       dbo.ComplexDocument
    Purpose:     Persistent state for the Complex module's simulated document API.
                 One row per document under management. Body content is stored
                 inline (NVARCHAR(MAX) for text, VARBINARY(MAX) reserved for
                 future binary support). Soft-delete via DeletedUtc lets demos
                 show realistic history without losing data.

    Related:     Issue #10 (Complex API simulation).
                 docs/complex-api.md for the design.
*/
CREATE TABLE dbo.ComplexDocument
(
    DocumentId          BIGINT IDENTITY(1,1)    NOT NULL,
    PublicId            UNIQUEIDENTIFIER        NOT NULL
        CONSTRAINT DF_ComplexDocument_PublicId DEFAULT (NEWID()),

    -- User-facing metadata
    [Name]              NVARCHAR(260)           NOT NULL,
    ContentType         NVARCHAR(128)           NOT NULL
        CONSTRAINT DF_ComplexDocument_ContentType DEFAULT ('text/plain'),
    SizeBytes           BIGINT                  NOT NULL
        CONSTRAINT DF_ComplexDocument_SizeBytes DEFAULT (0),

    -- Body inline. Replace with blob reference when bytes get serious.
    [Content]           NVARCHAR(MAX)           NULL,

    -- Lineage
    CreatedUtc          DATETIME2               NOT NULL
        CONSTRAINT DF_ComplexDocument_CreatedUtc DEFAULT (SYSUTCDATETIME()),
    ModifiedUtc         DATETIME2               NOT NULL
        CONSTRAINT DF_ComplexDocument_ModifiedUtc DEFAULT (SYSUTCDATETIME()),
    DeletedUtc          DATETIME2               NULL,
    CreatedBy           NVARCHAR(128)           NOT NULL
        CONSTRAINT DF_ComplexDocument_CreatedBy DEFAULT ('system'),

    -- Optimistic concurrency token
    [Version]           ROWVERSION              NOT NULL,

    CONSTRAINT PK_ComplexDocument
        PRIMARY KEY CLUSTERED (DocumentId),

    CONSTRAINT UQ_ComplexDocument_PublicId
        UNIQUE NONCLUSTERED (PublicId),

    CONSTRAINT CK_ComplexDocument_SizeBytes_NonNegative
        CHECK (SizeBytes >= 0),

    -- Most queries scope to live documents and order by recency.
    INDEX IX_ComplexDocument_DeletedUtc_ModifiedUtc
        NONCLUSTERED (DeletedUtc, ModifiedUtc DESC)
        INCLUDE ([Name], ContentType, SizeBytes),

    -- Name search supports the `?name=` filter on list endpoint.
    INDEX IX_ComplexDocument_Name
        NONCLUSTERED ([Name])
        INCLUDE (DeletedUtc, ModifiedUtc)
);
GO

EXEC sys.sp_addextendedproperty
    @name       = N'MS_Description',
    @value      = N'Persistent state for the Complex module simulated document API. See docs/complex-api.md.',
    @level0type = N'SCHEMA',      @level0name = N'dbo',
    @level1type = N'TABLE',       @level1name = N'ComplexDocument';
GO
