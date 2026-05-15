#!/usr/bin/env bash

echo "PWD: $(pwd)"
echo "Running $0"
az version

cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Database/
dotnet build

# Configuration Variables
DACPAC_PATH="./bin/Debug/FileIt.Database.dacpac"

# Execute deployment using SqlPackage
sqlpackage /Action:Publish \
    /SourceFile:"$DACPAC_PATH" \
    /TargetDatabaseName:"$AZURE_SQL_DATABASE" \
    /TargetServerName:"localhost" \
    /TargetUser:"$LOCAL_SQL_ADMIN" \
    /TargetPassword:"$LOCAL_SQL_PASSWORD" \
    /TargetEncryptConnection:False \
    /TargetTrustServerCertificate:True \
    /p:AllowIncompatiblePlatform=True \
    /p:BlockOnPossibleDataLoss=False \
    /p:DropObjectsNotInSource=True \
    /p:ExcludeObjectTypes="Users;Logins;RoleMembership;Permissions"

