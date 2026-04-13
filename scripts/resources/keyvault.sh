#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

# the service principal retrieved here is referenced in the key vault
# access policy, but it is preferred to use RBAC, so this should change

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

local_dev_sp_id=$(az ad sp list --display-name $devops_spn --query "[0].id" -o tsv)


az deployment sub create \
    --location $region \
    --template-file ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/templates/keyvault_sub.bicep \
    --parameters \
        stem=$stem \
        location=$region \
        servicePrincipalId=$local_dev_sp_id 


logout_azure
echo "Done"
exit 0