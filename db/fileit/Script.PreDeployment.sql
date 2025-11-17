-- Create the database
CREATE DATABASE FileIt;
GO

-- Create a SQL Server login
CREATE LOGIN FileItDev WITH PASSWORD = '123qwe!@#QWE',
DEFAULT_DATABASE = FileIt,
CHECK_EXPIRATION = OFF, -- Set to ON for production environments
CHECK_POLICY = OFF; -- Set to ON for production environments
GO

-- Switch to the newly created database
USE FileItDev;
GO

-- Create a user in the new database and map it to the login
CREATE USER FileItDev FOR LOGIN FileItDev;
GO

-- Grant permissions to the user (e.g., db_owner)
EXEC sp_addrolemember N'db_owner', N'FileItDev';
GO-- This file contains SQL statements that will be executed before the build script.
