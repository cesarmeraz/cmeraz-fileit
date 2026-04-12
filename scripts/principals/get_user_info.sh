#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

# Get info for a user defined managed identity
nameEnding="services"
userName="mi-$stem-$nameEnding"
resourceGroupName="rg-$stem-$nameEnding"

az identity show --name $userName --resource-group $resourceGroupName

logout_azure
echo "Done"
exit 0