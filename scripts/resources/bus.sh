#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${apim_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

if [[ $(az group exists --name $bus_group_name) == "true" ]]; then
    echo "Deleting $bus_group_name"
    az group delete --name $bus_group_name --yes
fi

echo $region

az deployment sub create \
    --location $region \
    --template-file scripts/templates/bus_sub.bicep \
    --parameters \
        resourceName=$bus_name \
        resourceGroupName=$bus_group_name \
        stem=$stem \
        location=$region \
        deploymentName=$deployment_name 

logout_azure