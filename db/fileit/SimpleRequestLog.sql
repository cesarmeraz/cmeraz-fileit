CREATE TABLE [dbo].[SimpleRequestLog]
(
  [Id] INT NOT NULL PRIMARY KEY,
  Environment NVARCHAR(100) NOT NULL,
  Host NVARCHAR(100) NOT NULL,
  Agent NVARCHAR(100),
  BlobName NVARCHAR(100),
  Comment NVARCHAR(100),
  CreatedOn DATETIME NOT NULL
    CONSTRAINT DF_SimpleRequestLog_CreatedOn DEFAULT GETDATE() 
)
