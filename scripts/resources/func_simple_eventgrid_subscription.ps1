#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

login_azure

$nameEnding = "simple"

create_eventgrid_subscription `
    "mi-$stem-$nameEnding" `
    "rg-$stem-$nameEnding" `
    "fa-$stem-$nameEnding" `
    "SimpleWatcher" `
    "$nameEnding-ingest-sub" `
    "$nameEnding-source" `
    "$nameEnding-ingest-sub-topic"

logout_azure
Write-Host "Done"
exit 0
