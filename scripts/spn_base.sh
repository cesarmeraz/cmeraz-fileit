#!/usr/bin/env bash

# ------------------------------------------------------
# GENERAL AZURE SETTINGS AND LOCAL ENVIRONMENT VARIABLES
# ------------------------------------------------------
# Your subscription id in GUID form
sub_id="${SUBSCRIPTION_ID}"
#if subscription id not set in env var, fail
if [ -z "$sub_id" ]; then
    echo "Error: SUBSCRIPTION_ID environment variable not set"
    exit 1
fi

# ------------------------------------------------------



# ------------------------------------------------------
# SOLUTION ENVIRONMENT VARIABLES
# ------------------------------------------------------

# The full path to the parent folder containing
# child folders that contain the service principal
# certificates
cert_parent_path="${CERT_PARENT_PATH}"

# if cert parent path not set in env var, fail
if [ -z "$cert_parent_path" ]; then
    echo "Error: CERT_PARENT_PATH environment variable not set"
    exit 1
fi

# The name of a devops service principal dedicated to 
# running deployment scripts from this project or from
# pipelines like Jenkins
devops_spn="${FILEIT_DEVOPS_SERVICE_PRINCIPAL}"
# if devops service principal name not set in env var, fail
if [ -z "$devops_spn" ]; then
    echo "Error: FILEIT_DEVOPS_SERVICE_PRINCIPAL environment variable not set"
    exit 1
fi


# ------------------------------------------------------
# SERVICE PRINCIPAL FUNCTIONS
# ------------------------------------------------------

# create a service principal with a cert for devops operations
create_spn(){
    p_servicePrincipalName=$1
    p_role=$2

    # "service principal name: $p_servicePrincipalName"
    if [ -z "$p_servicePrincipalName" ];then
        echo "Missing service principal name"
        exit 1
    fi

    # Set variables
    scope="/subscriptions/$sub_id" 
    certPath="$cert_parent_path/$p_servicePrincipalName/"

    mkdir -p $certPath
    cd $certPath

    # "Delete pem files from $certPath"
    rm -f $certPath/*.pem

    # "List existing service principal records with same displayName"
    existingAppId=$(az ad app list \
                    --filter "displayName eq '$p_servicePrincipalName'" \
                    | jq -r .[0].appId)
    if [[ -n $existingAppId ]];then
        az ad app delete --id $existingAppId
    fi

    # "Creating SP for RBAC, with certificate"
    fileWithCertAndPrivateKey=$(az ad sp create-for-rbac \
                            --name $p_servicePrincipalName \
                            --role  "$p_role" \
                            --scopes $scope \
                            --create-cert | jq -r .fileWithCertAndPrivateKey)

    # "rename the pem"
    mv -f $fileWithCertAndPrivateKey $p_servicePrincipalName.pem


    sp_appId=$(az ad sp list \
                --display-name $p_servicePrincipalName \
                --output tsv --query "[].appId")
    echo "$sp_appId" # echo the return value
}
