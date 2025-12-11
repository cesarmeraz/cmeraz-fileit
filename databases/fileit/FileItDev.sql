
USE FileIt;
GO
CREATE USER FileItDev FOR LOGIN FileItDev;
GO
EXEC sp_addrolemember N'db_owner', N'FileItDev';
GO
EXEC sp_addrolemember N'db_datareader', N'FileItDev';
GO
EXEC sp_addrolemember N'db_datawriter', N'FileItDev';
GO-- This file contains SQL statements that will be executed after the build script.
