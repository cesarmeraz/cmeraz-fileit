. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

# Get info for a user defined managed identity
$nameEnding = "services"
$userName = "mi-$stem-$nameEnding"
$resourceGroupName = "rg-$stem-$nameEnding"

az identity show --name $userName --resource-group $resourceGroupName

logout_azure
Write-Host "Done"
exit 0
