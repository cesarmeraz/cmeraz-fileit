CREATE TABLE [dbo].[GeneralAuditLog]
(
	[Id]              [INT] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Message]         [NVARCHAR](MAX) NULL,
	[MessageTemplate] [NVARCHAR](MAX) NULL,
	[Level]           [NVARCHAR](MAX) NULL,
	[TimeStamp]       [DATETIME] NULL,
	[Exception]       [NVARCHAR](MAX) NULL,
	[Properties]      [NVARCHAR](MAX) NULL,
	[EnvironmentName] [NVARCHAR](100) NULL,
	[MachineName]     [NVARCHAR](100) NULL,
	[ApplicationName] [NVARCHAR](100) NULL,
	[Version]         [NVARCHAR](100) NULL,
	[Module]          [NVARCHAR](100) NULL,
	[SourceContext]   [NVARCHAR](100) NULL,
	[ClientRequestId] [NVARCHAR](100) NULL,
	[EventId]         [INT] NULL
)
