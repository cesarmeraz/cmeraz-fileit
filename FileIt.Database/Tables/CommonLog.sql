CREATE TABLE dbo.CommonLog(
	[Id]                INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
	[Message]           NVARCHAR(MAX)       NULL,
	MessageTemplate     NVARCHAR(MAX)       NULL,
	[Level]             NVARCHAR(100)       NULL,
	Exception           NVARCHAR(MAX)       NULL,
	Properties          NVARCHAR(MAX)       NULL,
	Environment			NVARCHAR(100)       NULL,
	MachineName         NVARCHAR(100)       NULL,
	Application			NVARCHAR(100)       NULL,
	ApplicationVersion  NVARCHAR(100)       NULL,
	InfrastructureVersion	NVARCHAR(100)       NULL,
	SourceContext       NVARCHAR(100)       NULL,
	CorrelationId	    NVARCHAR(100)       NULL,
	InvocationId	    NVARCHAR(100)       NULL,
	EventId             INT                 NULL,
    CreatedOn           DATETIME2           NOT NULL,
    ModifiedOn          DATETIME2           NOT NULL,
	EventName           NVARCHAR(100)       NULL
);
GO

CREATE NONCLUSTERED INDEX IX_CommonLog_EventName
    ON dbo.CommonLog(EventName)
    WHERE EventName IS NOT NULL;
GO