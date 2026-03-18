#!/usr/bin/env bash
. scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="${database_group_name}-${timestamp}"
echo "Deployment name: $deployment_name"

if [[ $(az group exists --name $database_group_name) == "true" ]]; then
    echo "Deleting $database_group_name"
    az group delete --name $database_group_name --yes
fi

echo $region

PUBLIC_IP=$(curl -s icanhazip.com)
echo "My public IP address is: $PUBLIC_IP"


read -p "Enter sql admin username: " sqlAdminUsername
read -sp "Enter sql admin password: " sqlAdminPassword
echo # Optional: moves the cursor to a new line after silent input
echo "Password stored securely."


az deployment sub create \
    --location $region \
    --name $deployment_name \
    --template-file scripts/templates/database_sub.bicep \
    --parameters \
        resourceName=$database_name \
        resourceGroupName=$database_group_name \
        stem=$stem \
        location=$region \
        deploymentName=$deployment_name \
        adminLogin=$sqlAdminUsername \
        adminPassword=$sqlAdminPassword \
        myLocalIpAddress=$PUBLIC_IP

logout_azure