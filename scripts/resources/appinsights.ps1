#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deployment_name = "$appinsights_group_name-$timestamp"
Write-Host "Deployment name: $deployment_name"

# if ((az group exists --name $appinsights_group_name) -eq "true") {
#     Write-Host "Deleting $appinsights_group_name"
#     az group delete --name $appinsights_group_name --yes
# }

az deployment sub create `
    --location $region `
    --template-file "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/templates/appinsights_sub.bicep" `
    --name $deployment_name `
    --parameters `
        location=$region `
        stem=$stem `
        resourceGroupName=$appinsights_group_name `
        resourceName=$appinsights_name `
        workspaceName="$appinsights_name-prod" `
        deploymentName=$deployment_name


logout_azure
Write-Host "Done"
exit 0
