#!/bin/bash
. scripts/base.sh

# This script creates a service principal with 
# certificate credentials as the service account for
# the azcopy utility. Save the principalId output in your 
# environment variable AZCOPY_CLIENT_ID, e.g. launch.json.
# Log in as owner to create this spn.

# Bash script
echo "Running $0 script"

# Are we reading in a service principal name from env var?
servicePrincipalName=$azcopy_spn
echo "servicePrincipalName: $servicePrincipalName"
if [ -z "$servicePrincipalName" ];then
    echo "No service principal name found in env var WEB_SERVICE_PRINCIPAL"
    exit 1
fi


# Set variables
location=$region
scope="/subscriptions/$sub_id" 
certPath="$cert_parent_path/$servicePrincipalName/"

mkdir -p $certPath
cd $certPath

echo "Delete pem files from $certPath"
rm -f $certPath/*.pem

echo "$PWD"

echo "log in as owner"
az login

echo "List existing service principal records with same displayName"
existingAppId=$(az ad app list --filter "displayName eq '$servicePrincipalName'" | jq -r .[0].appId)
if [ $existingAppId != "null" ];then
    az ad app delete --id $existingAppId
fi

echo "Creating SP for RBAC, with certificate"
fileWithCertAndPrivateKey=$(az ad sp create-for-rbac --name $servicePrincipalName \
                         --role "Storage Blob Data Contributor" \
                         --scopes $scope \
                         --create-cert | jq -r .fileWithCertAndPrivateKey)

echo "rename the pem"
mv -f $fileWithCertAndPrivateKey $servicePrincipalName.pem


principalId=$(az ad sp list --display-name $servicePrincipalName --output tsv --query "[].id")
echo "principalId for $servicePrincipalName: $principalId"
