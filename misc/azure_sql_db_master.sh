IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'fileit-common')
    DROP USER [fileit-common];
GO

CREATE USER [fileit-common] FROM EXTERNAL PROVIDER WITH DEFAULT_SCHEMA=[dbo];
GO
