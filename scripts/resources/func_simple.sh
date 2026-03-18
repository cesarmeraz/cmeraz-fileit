#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
resource_name="$stem-simple"
resource_group_name="rg-$resource_name"
deployment_name="$resource_group_name-$timestamp"
echo "Deployment name: $deployment_name"

if [[ $(az group exists --name $resource_group_name) == "true" ]]; then
    echo "Deleting $resource_group_name"
    az group delete --name $resource_group_name --yes
fi

az deployment sub create \
    --name $deployment_name \
    --location $region \
    --template-file scripts/templates/func_sub.bicep \
    --parameters \
        resourceName=$resource_name \
        resourceGroupName=$resource_group_name \
        stem=$stem \
        location=$region \
        deploymentName=$deployment_name 

logout_azure