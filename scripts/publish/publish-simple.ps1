#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

$resource_name = "fa-$stem-simple"
$resource_group_name = "rg-$stem-simple"

Set-Location "$env:FILEIT_REPO_HOME/cmeraz-fileit/FileIt.Module.SimpleFlow"
dotnet publish --configuration Release

az functionapp deployment source config-zip `
  -g $resource_group_name `
  -n $resource_name `
  --src ./FileIt.Module.SimpleFlow.Host/bin/Release/net10.0/FileIt.Module.SimpleFlow.Host.zip

logout_azure
