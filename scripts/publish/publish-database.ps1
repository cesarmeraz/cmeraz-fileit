#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

Set-Location "$env:FILEIT_REPO_HOME/cmeraz-fileit/FileIt.Database/"
dotnet build

# Configuration Variables
$DACPAC_PATH = "./bin/Debug/fileit.dacpac"

# For Azure SQL, use a connection string for better control
$CONN_STR = "Server=tcp:$sql_server_name.database.windows.net;Database=$database_name;Authentication=Active Directory Interactive;Encrypt=True;"

# Execute deployment using SqlPackage
sqlpackage /Action:Publish `
    /SourceFile:"$DACPAC_PATH" `
    /TargetConnectionString:"$CONN_STR" `
    /p:AllowIncompatiblePlatform=True
