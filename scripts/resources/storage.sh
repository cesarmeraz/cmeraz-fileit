#!/usr/bin/env bash

. ./scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
az login

if [[ $(az group exists --name $storage_group_name) == "true" ]]; then
    echo "Deleting $storage_group_name"
    az group delete --name $storage_group_name --yes
fi

az deployment sub create \
    --location $region \
    --template-file templates/storage_sub.bicep \
    --parameters \
        stem=$stem \
        location=$region