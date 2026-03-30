-- Create user in master database for managed identity
USE master;
GO

DROP USER IF EXISTS [mi-fileit-common];
GO
CREATE USER [mi-fileit-common] FROM EXTERNAL PROVIDER;
GO

DROP USER IF EXISTS [mi-fileit-simple];
GO
CREATE USER [mi-fileit-simple] FROM EXTERNAL PROVIDER;
GO
