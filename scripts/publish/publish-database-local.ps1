#!/usr/bin/env pwsh

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

Set-Location "$env:FILEIT_REPO_HOME/cmeraz-fileit/database/fileit/"
dotnet build

# Configuration Variables
$DACPAC_PATH = "./bin/Debug/fileit.dacpac"

# For Azure SQL, use a connection string for better control
$CONN_STR = "Server=localhost;Database=$env:AZURE_SQL_DATABASE;User Id=$env:LOCAL_SQL_ADMIN;Password=$env:LOCAL_SQL_PASSWORD;Encrypt=True;"

# Execute deployment using SqlPackage
sqlpackage /Action:Publish `
    /SourceFile:"$DACPAC_PATH" `
    /TargetConnectionString:"$CONN_STR" `
    /p:AllowIncompatiblePlatform=True
