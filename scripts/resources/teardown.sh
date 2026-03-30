#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

if [[ $(az group exists --name $appinsights_group_name) == "true" ]]; then
    echo "Deleting $appinsights_group_name"
    az group delete --name $appinsights_group_name --yes
fi

if [[ $(az group exists --name $bus_group_name) == "true" ]]; then
    echo "Deleting $bus_group_name"
    az group delete --name $bus_group_name --yes
fi

if [[ $(az group exists --name $database_group_name) == "true" ]]; then
    echo "Deleting $database_group_name"
    az group delete --name $database_group_name --yes
fi

if [[ $(az group exists --name $keyvault_group_name) == "true" ]]; then
    echo "Deleting $keyvault_group_name"
    az group delete --name $keyvault_group_name --yes
fi

if [[ $(az group exists --name $storage_group_name) == "true" ]]; then
    echo "Deleting $storage_group_name"
    az group delete --name $storage_group_name --yes
fi

logout_azure
echo "Done"
exit 0