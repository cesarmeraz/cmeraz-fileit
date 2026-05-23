#!/usr/bin/env bash
echo "PWD: $(pwd)"
echo "Running $0"

# Configuration found in Environment Variables
PROJECT_PATH="${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Database/"
DACPAC_PATH="$PROJECT_PATH/bin/Debug/FileIt.Database.dacpac"
TARGET_CONNECTION_STRING="Server=tcp:${AZURE_SQL_SERVER}.database.windows.net,1433;Initial Catalog=${AZURE_SQL_DATABASE};Authentication=Active Directory Interactive;Encrypt=True;TrustServerCertificate=True;"

# Build the project to generate the DACPAC
dotnet build

# Deploy using sqlpackage with all options as command-line properties
# If sqlpackage errors, diagnose with the /Diagnostics and /DiagnosticsLevel parameters

sqlpackage /Action:Publish \
    /SourceFile:"$DACPAC_PATH" \
    /TargetConnectionString:"$TARGET_CONNECTION_STRING" \
    /p:AllowIncompatiblePlatform=False \
    /p:BlockOnPossibleDataLoss=False \
    /p:CreateNewDatabase=False \
    /p:DropObjectsNotInSource=True \
    /p:DoNotDropObjectTypes="Users;Logins;RoleMembership;Permissions"

