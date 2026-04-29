# deploy-database.ps1
# Publishes FileIt.Database.dacpac to the FileIt database.
# Idempotent: safe to run repeatedly. sqlpackage diffs the DACPAC against the
# live schema and applies only the changes.
#
# Prereqs:
#   - sqlpackage installed (dotnet tool install -g microsoft.sqlpackage)
#   - $env:FILEIT_DB_PASSWORD set to the SQL login password
#   - DACPAC built (dotnet build FileIt.Database/FileIt.Database.sqlproj)
#
# Usage:
#   .\scripts\deploy-database.ps1
#   .\scripts\deploy-database.ps1 -Server other.database.windows.net -Database FileIt_UAT

[CmdletBinding()]
param(
    [string]$Server     = "jmplabsv04.database.windows.net",
    [string]$Database   = "FileIt",
    [string]$User       = "Proximus",
    [string]$DacpacPath = (Join-Path $PSScriptRoot "..\FileIt.Database\bin\Debug\FileIt.Database.dacpac"),
    [switch]$ReportOnly
)

$ErrorActionPreference = "Stop"

if (-not $env:FILEIT_DB_PASSWORD) {
    throw "FILEIT_DB_PASSWORD env var not set. Run: `$env:FILEIT_DB_PASSWORD = 'your_password' (use single quotes if password contains `$)"
}

$resolvedDacpac = Resolve-Path -Path $DacpacPath -ErrorAction SilentlyContinue
if (-not $resolvedDacpac) {
    throw "DACPAC not found at $DacpacPath. Run: dotnet build FileIt.Database/FileIt.Database.sqlproj"
}

if (-not (Get-Command sqlpackage -ErrorAction SilentlyContinue)) {
    throw "sqlpackage not on PATH. Install: dotnet tool install -g microsoft.sqlpackage"
}

$connStr = "Server=tcp:$Server,1433;Initial Catalog=$Database;User ID=$User;Password=$($env:FILEIT_DB_PASSWORD);Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;"

Write-Host ""
Write-Host "Target: $User@$Server / $Database" -ForegroundColor Cyan
Write-Host "Source: $($resolvedDacpac.Path)" -ForegroundColor DarkGray
Write-Host ""

if ($ReportOnly) {
    $reportPath = Join-Path (Split-Path -Parent $PSScriptRoot) "db-deploy-report.xml"
    & sqlpackage /Action:DeployReport `
        /SourceFile:"$($resolvedDacpac.Path)" `
        /TargetConnectionString:"$connStr" `
        /OutputPath:"$reportPath"
    Write-Host ""
    Write-Host "Report written to: $reportPath" -ForegroundColor Green
    return
}

& sqlpackage /Action:Publish `
    /SourceFile:"$($resolvedDacpac.Path)" `
    /TargetConnectionString:"$connStr" `
    /p:BlockOnPossibleDataLoss=true `
    /p:GenerateSmartDefaults=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "sqlpackage failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Database publish complete." -ForegroundColor Green
