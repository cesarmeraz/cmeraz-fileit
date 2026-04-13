#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

if ((az group exists --name $appinsights_group_name) -eq "true") {
    Write-Host "Deleting $appinsights_group_name"
    az group delete --name $appinsights_group_name --yes
}

if ((az group exists --name $bus_group_name) -eq "true") {
    Write-Host "Deleting $bus_group_name"
    az group delete --name $bus_group_name --yes
}

if ((az group exists --name $database_group_name) -eq "true") {
    Write-Host "Deleting $database_group_name"
    az group delete --name $database_group_name --yes
}

if ((az group exists --name $keyvault_group_name) -eq "true") {
    Write-Host "Deleting $keyvault_group_name"
    az group delete --name $keyvault_group_name --yes
}

if ((az group exists --name $storage_group_name) -eq "true") {
    Write-Host "Deleting $storage_group_name"
    az group delete --name $storage_group_name --yes
}

if ((az group exists --name "rg-$stem-services") -eq "true") {
    Write-Host "Deleting rg-$stem-services"
    az group delete --name "rg-$stem-services" --yes
}

if ((az group exists --name "rg-$stem-simple") -eq "true") {
    Write-Host "Deleting rg-$stem-simple"
    az group delete --name "rg-$stem-simple" --yes
}

logout_azure
Write-Host "Done"
exit 0
