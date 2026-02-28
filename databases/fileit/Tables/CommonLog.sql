CREATE TABLE dbo.CommonLog(
	[Id]                INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
	[Message]           NVARCHAR(MAX)       NULL,
	MessageTemplate     NVARCHAR(MAX)       NULL,
	[Level]             NVARCHAR(100)       NULL,
	Exception           NVARCHAR(MAX)       NULL,
	Properties          NVARCHAR(MAX)       NULL,
	Environment			NVARCHAR(100)       NULL,
	MachineName         NVARCHAR(100)       NULL,
	Feature			    NVARCHAR(100)       NULL,
	FeatureVersion      NVARCHAR(100)       NULL,
	CommonVersion       NVARCHAR(100)       NULL,
	SourceContext       NVARCHAR(100)       NULL,
	CorrelationId	    NVARCHAR(100)       NULL,
	EventId             INT                 NULL,
    CreatedOn           DATETIME2           NOT NULL,
    ModifiedOn          DATETIME2           NOT NULL
)