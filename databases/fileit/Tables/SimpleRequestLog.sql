CREATE TABLE [dbo].[SimpleRequestLog]
(
  [Id] INT NOT NULL PRIMARY KEY,
  ClientRequestId NVARCHAR(100) NOT NULL,
  ApiId INT NULL CONSTRAINT DF_SimpleRequestLog_ApiId DEFAULT 0,
  [Status] NVARCHAR(100) NOT NULL CONSTRAINT DF_SimpleRequestLog_Status DEFAULT 'New',
  CreatedOn DATETIME NOT NULL CONSTRAINT DF_SimpleRequestLog_CreatedOn DEFAULT GETDATE(),
  ModifiedOn DATETIME NOT NULL CONSTRAINT DF_SimpleRequestLog_ModifiedOn DEFAULT GETDATE()
)
