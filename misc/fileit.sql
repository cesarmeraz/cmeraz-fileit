-- Write your own SQL object definition here, and it'll be included in your package.
CREATE DATABASE FileIt;
GO

-- Create a SQL Server login
CREATE LOGIN FileItDev WITH PASSWORD = '123qwe!@#QWE',
DEFAULT_DATABASE = FileIt,
CHECK_EXPIRATION = OFF, -- Set to ON for production environments
CHECK_POLICY = OFF; -- Set to ON for production environments
GO

-- Switch to the newly created database
USE FileIt;
GO

-- Create a user in the new database and map it to the login
USE FileIt; -- Switch to the database where the user will be created
GO
CREATE USER FileItDev FOR LOGIN FileItDev;
GO

-- Grant permissions to the user (e.g., db_owner)
EXEC sp_addrolemember N'db_owner', N'FileItDev';
GO
EXEC sp_addrolemember N'db_datareader', N'FileItDev';
GO
EXEC sp_addrolemember N'db_datawriter', N'FileItDev';
GO

use master;
GRANT VIEW ANY DEFINITION TO [FileItDev];
GO