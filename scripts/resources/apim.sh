#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

if [[ $(az group exists --name $apim_group_name) == "true" ]]; then
    echo "Deleting $apim_group_name"
    az group delete --name $apim_group_name --yes
fi

local_dev_sp_id=$(az ad sp list --display-name "${LOCALDEV_SERVICE_PRINCIPAL}" --query "[0].id" -o tsv)


az deployment sub create \
    --location $region \
    --template-file templates/apim_sub.bicep \
    --parameters \
        stem=$stem \
        location=$region

# log out of owner account
az logout