#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

# the service principal retrieved here is referenced in the key vault
# access policy, but it is preferred to use RBAC, so this should change

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

$local_dev_sp_id = az ad sp list --display-name $devops_spn --query "[0].id" -o tsv

az deployment sub create `
    --location $region `
    --template-file "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/templates/keyvault_sub.bicep" `
    --parameters `
        stem=$stem `
        location=$region `
        servicePrincipalId=$local_dev_sp_id

logout_azure
Write-Host "Done"
exit 0
