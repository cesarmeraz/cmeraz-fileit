-- Create user in master database for managed identity
USE master;
GO

DROP USER IF EXISTS [mi-fileit-services];
GO
CREATE USER [mi-fileit-services] FROM EXTERNAL PROVIDER;
GO

DROP USER IF EXISTS [mi-fileit-simple];
GO
CREATE USER [mi-fileit-simple] FROM EXTERNAL PROVIDER;
GO
