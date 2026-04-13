#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version

login_azure

# create_queue 'api-add'
$p_queueName = "api-add"

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deployment_name = "$bus_group_name-$p_queueName-$timestamp"
Write-Host "Deployment name: $deployment_name"

az deployment group create `
    --resource-group $bus_group_name `
    --name $deployment_name `
    --template-file "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/templates/bus_queue.bicep" `
    --parameters `
        namespace=$bus_name `
        name=$p_queueName

logout_azure
Write-Host "Done"
exit 0
