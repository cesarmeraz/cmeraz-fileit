#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

timestamp=$(date +"%Y%m%d-%H%M%S")
resource_name="$stem-simple"
resource_group_name="rg-$resource_name"

login_azure

az deployment group create \
    --name "$resource_name-eventgridcontributor-$timestamp" \
    --resource-group $storage_group_name \
    --template-file scripts/templates/func_rbac_eventgridcontributor.bicep \
    --parameters \
        functionAppName=$resource_name \
        functionRGName=$resource_group_name \
        storageAccountName=$storage_name \
        eventGridTopicName="$storage_name-$resource_name-topic" 

logout_azure
echo "Done"
exit 0