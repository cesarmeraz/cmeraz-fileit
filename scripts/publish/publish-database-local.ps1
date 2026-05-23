#!/usr/bin/env pwsh

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

Set-Location "$env:FILEIT_REPO_HOME/cmeraz-fileit/FileIt.Database/"
dotnet build

# Configuration Variables
$DACPAC_PATH = "./bin/Debug/FileIt.Database.dacpac"

# Execute deployment using SqlPackage
sqlpackage /Action:Publish `
    /TargetDatabaseName:"$env:AZURE_SQL_DATABASE" `
    /TargetServerName:"$env:LOCAL_SQL_SERVER" `
    /TargetUser:"$env:LOCAL_SQL_ADMIN" `
    /TargetPassword:"$env:LOCAL_SQL_PASSWORD" `
    /SourceFile:"$DACPAC_PATH" `
    /p:AllowIncompatiblePlatform=True
