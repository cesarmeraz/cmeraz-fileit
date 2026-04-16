#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deployment_name = "$bus_group_name-$timestamp"
Write-Host "Deployment name: $deployment_name"

az deployment sub create `
    --location $region `
    --template-file "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/templates/bus_sub.bicep" `
    --name $deployment_name `
    --parameters `
        resourceName=$bus_name `
        resourceGroupName=$bus_group_name `
        stem=$stem `
        location=$region `
        deploymentName=$deployment_name

logout_azure
Write-Host "Done"
exit 0
