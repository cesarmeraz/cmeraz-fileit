#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deployment_name = "$storage_group_name-$timestamp"
Write-Host "Deployment name: $deployment_name"

login_azure

az deployment sub create `
    --name $deployment_name `
    --location $region `
    --template-file "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/templates/storage_sub.bicep" `
    --parameters `
        name=$storage_name `
        group_name=$storage_group_name `
        stem=$stem `
        location=$region `
        deployment_name=$deployment_name

logout_azure
Write-Host "Done"
exit 0
