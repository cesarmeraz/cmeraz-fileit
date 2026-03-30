#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${appinsights_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

# if [[ $(az group exists --name $appinsights_group_name) == "true" ]]; then
#     echo "Deleting $appinsights_group_name"
#     az group delete --name $appinsights_group_name --yes
# fi

az deployment sub create \
    --location $region \
    --template-file scripts/templates/appinsights_sub.bicep \
    --name $deployment_name \
    --parameters \
        location=$region \
        stem=$stem \
        resourceGroupName=$appinsights_group_name \
        resourceName=$appinsights_name \
        workspaceName="$appinsights_name-prod" \
        deploymentName=$deployment_name 


logout_azure
echo "Done"
exit 0