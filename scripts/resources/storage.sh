#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${storage_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

login_azure


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
echo "Done"
exit 0