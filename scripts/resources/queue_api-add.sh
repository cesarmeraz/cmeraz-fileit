#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version


login_azure

# create_queue 'api-add'
p_queueName="api-add"

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_queueName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/templates/bus_queue.bicep \
        --parameters \
            namespace=$bus_name \
            name=$p_queueName

logout_azure
echo "Done"
exit 0