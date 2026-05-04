#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Database/
dotnet build

# Configuration Variables
DACPAC_PATH="./bin/Debug/FileIt.Database.dacpac"
PROFILE_PATH="./fileit_azure.publish.xml"

# Execute deployment using SqlPackage
# /ua:True triggers Azure AD Interactive (Universal Authentication) — no password needed.
sqlpackage /Action:Publish \
    /SourceFile:"$DACPAC_PATH" \
    /Profile:"$PROFILE_PATH" \
    /TargetServerName:"${sql_server_name}.database.windows.net" \
    /TargetDatabaseName:"${database_name}" \
    /ua:True
