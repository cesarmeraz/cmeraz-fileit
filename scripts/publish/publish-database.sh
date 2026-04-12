#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

cd ${FILEIT_REPO_HOME}/cmeraz-fileit/database/fileit/
dotnet build

# Configuration Variables
DACPAC_PATH="./bin/Debug/fileit.dacpac"

# For Azure SQL, use a connection string for better control
CONN_STR="Server=tcp:$sql_server_name.database.windows.net;Database=$database_name;Authentication=Active Directory Interactive;Encrypt=True;"

# Execute deployment using SqlPackage
sqlpackage /Action:Publish \
    /SourceFile:"$DACPAC_PATH" \
    /TargetConnectionString:"$CONN_STR" \
    /p:AllowIncompatiblePlatform=True
