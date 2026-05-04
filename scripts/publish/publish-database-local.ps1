#!/usr/bin/env pwsh

if ([string]::IsNullOrEmpty($env:FILEIT_REPO_HOME) -or
    [string]::IsNullOrEmpty($env:AZURE_SQL_DATABASE) -or
    [string]::IsNullOrEmpty($env:LOCAL_SQL_ADMIN) -or
    [string]::IsNullOrEmpty($env:LOCAL_SQL_PASSWORD)) {
    Write-Host "Error: Missing required env var(s). Expected FILEIT_REPO_HOME, AZURE_SQL_DATABASE, LOCAL_SQL_ADMIN, LOCAL_SQL_PASSWORD."
    exit 1
}

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

Set-Location "$env:FILEIT_REPO_HOME/cmeraz-fileit/FileIt.Database/"
dotnet build

# Configuration Variables
$DACPAC_PATH = "./bin/Debug/FileIt.Database.dacpac"
$PROFILE_PATH = "./fileit_local.publish.xml"

# Execute deployment using SqlPackage
# Credentials are passed as separate flags to avoid embedding them in a
# connection string (which would expose them via process listing).
sqlpackage /Action:Publish `
    /SourceFile:"$DACPAC_PATH" `
    /Profile:"$PROFILE_PATH" `
    /TargetServerName:"localhost" `
    /TargetTrustServerCertificate:True `
    /TargetDatabaseName:"$env:AZURE_SQL_DATABASE" `
    /TargetUser:"$env:LOCAL_SQL_ADMIN" `
    /TargetPassword:"$env:LOCAL_SQL_PASSWORD"
