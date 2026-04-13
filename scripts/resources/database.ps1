#!/usr/bin/env pwsh
. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deployment_name = "$database_group_name-$timestamp"
Write-Host "Deployment name: $deployment_name"

$PUBLIC_IP = (Invoke-RestMethod -Uri "https://icanhazip.com").Trim()
Write-Host "My public IP address is: $PUBLIC_IP"

$sqlAdminUsername = Read-Host "Enter sql admin username"
$sqlAdminPassword = Read-Host "Enter sql admin password" -AsSecureString
$sqlAdminPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlAdminPassword))
Write-Host "Password stored securely."

az deployment sub create `
    --location $region `
    --name $deployment_name `
    --template-file "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/templates/database_sub.bicep" `
    --parameters `
        resourceName=$database_name `
        resourceGroupName=$database_group_name `
        stem=$stem `
        location=$region `
        deploymentName=$deployment_name `
        adminLogin=$sqlAdminUsername `
        adminPassword=$sqlAdminPasswordPlain `
        myLocalIpAddress=$PUBLIC_IP

logout_azure
Write-Host "Done"
exit 0
