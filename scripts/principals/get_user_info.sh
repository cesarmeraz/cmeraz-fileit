#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

# Get info for a user defined managed identity
nameEnding="common"
userName="mi-$stem-$nameEnding"
resourceGroupName="rg-$stem-$nameEnding"

# set variable with clientId of the user assigned identity
userIdentityClientId=$(az identity show --name $userName --resource-group $resourceGroupName --query "clientId" -o tsv)

logout_azure
echo "Done"
exit 0