-- Create user in application database and grant roles
DROP USER IF EXISTS [mi-fileit-services];
CREATE USER [mi-fileit-services] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [mi-fileit-services];
ALTER ROLE db_datareader ADD MEMBER [mi-fileit-services];
ALTER ROLE db_datawriter ADD MEMBER [mi-fileit-services];
DROP USER IF EXISTS [mi-fileit-simple];
CREATE USER [mi-fileit-simple] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [mi-fileit-simple];
ALTER ROLE db_datareader ADD MEMBER [mi-fileit-simple];
ALTER ROLE db_datawriter ADD MEMBER [mi-fileit-simple];