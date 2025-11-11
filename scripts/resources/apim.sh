#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${apim_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

if [[ $(az group exists --name $apim_group_name) == "true" ]]; then
    echo "Deleting $apim_group_name"
    az group delete --name $apim_group_name --yes
fi

az deployment sub create \
    --name $deployment_name \
    --location $region \
    --template-file templates/apim_sub.bicep \
    --parameters \
        resourceName=$apim_name \
        resourceGroupName=$apim_group_name \
        stem=$stem \
        location=$region \
        deploymentName=$deployment_name 

logout_azure