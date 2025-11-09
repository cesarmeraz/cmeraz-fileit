#!/usr/bin/env bash

. ./scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${storage_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

login_azure

if [[ $(az group exists --name $storage_group_name) == "true" ]]; then
    echo "Deleting $storage_group_name"
    az group delete --name $storage_group_name --yes
fi

az deployment sub create \
    --name $deployment_name \
    --location $region \
    --template-file scripts/templates/storage_sub.bicep \
    --parameters \
        name=$storage_name \
        group_name=$storage_group_name \
        stem=$stem \
        location=$region \
        deployment_name=$deployment_name 

logout_azure