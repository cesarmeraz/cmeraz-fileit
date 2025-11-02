#!/usr/bin/env bash
. scripts/base.sh

# the service principal retrieved here is referenced in the key vault
# access policy, but it is preferred to use RBAC, so this should change

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

if [[ $(az group exists --name $keyvault_group_name) == "true" ]]; then
    echo "Deleting $keyvault_group_name"
    az group delete --name $keyvault_group_name --yes
fi

local_dev_sp_id=$(az ad sp list --display-name $devops_spn --query "[0].id" -o tsv)


az deployment sub create \
    --location $region \
    --template-file templates/keyvault_sub.bicep \
    --parameters \
        stem=$stem \
        location=$region \
        servicePrincipalId=$local_dev_sp_id 
