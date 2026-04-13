#!/usr/bin/env bash

echo "PWD: $(pwd)"
echo "Running $0"
az version

cd ${FILEIT_REPO_HOME}/cmeraz-fileit/database/fileit/
dotnet build

# Configuration Variables
DACPAC_PATH="./bin/Debug/fileit.dacpac"

# For Azure SQL, use a connection string for better control
CONN_STR="Server=localhost;Database=${AZURE_SQL_DATABASE};User Id=${LOCAL_SQL_ADMIN};Password=${LOCAL_SQL_PASSWORD};Encrypt=True;"

# Execute deployment using SqlPackage
sqlpackage /Action:Publish \
    /SourceFile:"$DACPAC_PATH" \
    /TargetConnectionString:"$CONN_STR" \
    /p:AllowIncompatiblePlatform=True
