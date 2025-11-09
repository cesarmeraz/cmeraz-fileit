#!/usr/bin/env bash
    . scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${function_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

if [[ $(az group exists --name $function_group_name) == "true" ]]; then
    echo "Deleting $function_group_name"
    az group delete --name $function_group_name --yes
fi

az deployment sub create \
    --name $deployment_name \
    --location $region \
    --template-file scripts/templates/func_sub.bicep \
    --parameters \
        resourceName=$storage_name \
        resourceGroupName=$function_group_name \
        stem=$stem \
        location=$region \
        deploymentName=$deployment_name 

logout_azure