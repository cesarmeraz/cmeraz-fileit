#!/usr/bin/env bash

# VS Code tasks often run non-login shells; load system profile so /etc/profile.d
# environment variables are available for this script.
if [ -f /etc/profile ]; then
    # shellcheck source=/etc/profile
    . /etc/profile
fi

if [ -z "${FILEIT_REPO_HOME}" ] || [ -z "${AZURE_SQL_DATABASE}" ] || [ -z "${LOCAL_SQL_ADMIN}" ] || [ -z "${LOCAL_SQL_PASSWORD}" ]; then
    echo "Error: Missing required env var(s). Expected FILEIT_REPO_HOME, AZURE_SQL_DATABASE, LOCAL_SQL_ADMIN, LOCAL_SQL_PASSWORD."
    exit 1
fi

echo "PWD: $(pwd)"
echo "Running $0"
az version

cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Database/
dotnet build

# Configuration Variables
DACPAC_PATH="./bin/Debug/FileIt.Database.dacpac"
PROFILE_PATH="./fileit_local.publish.xml"

# Execute deployment using SqlPackage
# Credentials are passed as separate flags to avoid embedding them in a
# connection string (which would expose them via 'ps aux').
sqlpackage /Action:Publish \
    /SourceFile:"$DACPAC_PATH" \
    /Profile:"$PROFILE_PATH" \
    /TargetServerName:"localhost" \
    /TargetTrustServerCertificate:True \
    /TargetDatabaseName:"${AZURE_SQL_DATABASE}" \
    /TargetUser:"${LOCAL_SQL_ADMIN}" \
    /TargetPassword:"${LOCAL_SQL_PASSWORD}"
