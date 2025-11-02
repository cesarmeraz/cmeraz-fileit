#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

if [[ $(az group exists --name $bus_group_name) == "true" ]]; then
    echo "Deleting $bus_group_name"
    az group delete --name $bus_group_name --yes
fi

az deployment sub create \
    --location $region \
    --template-file templates/bus_sub.bicep \
    --parameters \
        stem=$stem \
        location=$region