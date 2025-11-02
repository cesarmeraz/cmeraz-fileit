#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

if [[ $(az group exists --name $web_group_name) == "true" ]]; then
    echo "Deleting $web_group_name"
    az group delete --name $web_group_name --yes
fi

az deployment sub create \
    --location $region \
    --template-file templates/web_sub.bicep \
    --parameters \
        stem=$stem \
        location=$region