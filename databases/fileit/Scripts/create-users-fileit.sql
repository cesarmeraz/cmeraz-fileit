-- Create user in application database and grant roles
DROP USER IF EXISTS [mi-fileit-common];
CREATE USER [mi-fileit-common] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [mi-fileit-common];
ALTER ROLE db_datareader ADD MEMBER [mi-fileit-common];
ALTER ROLE db_datawriter ADD MEMBER [mi-fileit-common];
DROP USER IF EXISTS [mi-fileit-simple];
CREATE USER [mi-fileit-simple] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [mi-fileit-simple];
ALTER ROLE db_datareader ADD MEMBER [mi-fileit-simple];
ALTER ROLE db_datawriter ADD MEMBER [mi-fileit-simple];