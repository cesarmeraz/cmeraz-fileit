#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${bus_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

az deployment sub create \
    --location $region \
    --template-file ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/templates/bus_sub.bicep \
    --name $deployment_name \
    --parameters \
        resourceName=$bus_name \
        resourceGroupName=$bus_group_name \
        stem=$stem \
        location=$region \
        deploymentName=$deployment_name 

logout_azure
echo "Done"
exit 0