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


# Your tenant id in GUID form
tenant_id="${TENANT_ID}"
#if tenant id not set in env var, fail
if [ -z "$tenant_id" ]; then
    echo "Error: TENANT_ID environment variable not set"
    exit 1
fi


# The server name, not the fully qualified domain name
sql_server_name="${AZURE_SQL_SERVER}"
#if sql server name not set in env var, fail
if [ -z "$sql_server_name" ]; then
    echo "Error: AZURE_SQL_SERVER environment variable not set"
    exit 1
fi



# The name of the database on the server, FileIt is mine
database_name="${AZURE_SQL_DATABASE}"
#if database name not set in env var, fail
if [ -z "$database_name" ]; then
    echo "Error: AZURE_SQL_DATABASE environment variable not set"
    exit 1
fi

# The root of the repo on the local dev box / build agent
repo_home="${FILEIT_REPO_HOME}"
#if repo home not set in env var, fail
if [ -z "$repo_home" ]; then
    echo "Error: FILEIT_REPO_HOME environment variable not set"
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


# The devops service principal id, for authentication
# In Entra ID, this is the Application (client) ID of the SPN
# found in App registrations
devops_client_id="${FILEIT_DEVOPS_CLIENT_ID}"
# if devops client id not set in env var, fail
if [ -z "$devops_client_id" ]; then
    echo "Error: FILEIT_DEVOPS_CLIENT_ID environment variable not set"
    exit 1
fi


# The name of a devops service principal dedicated to 
# running deployment scripts from this project or from
# pipelines like Jenkins
devops_spn=$FILEIT_DEVOPS_SERVICE_PRINCIPAL
# if devops service principal name not set in env var, fail
if [ -z "$devops_spn" ]; then
    echo "Error: FILEIT_DEVOPS_SERVICE_PRINCIPAL environment variable not set"
    exit 1
fi

# The location to the devops certificate, for authentication
devops_client_key_path="$cert_parent_path/$devops_spn/$devops_spn.pem"

# A single region for all resource groups in this project
# resources inherit their location value from their resource groups
region="${FILEIT_REGION}"
# if region not set in env var, fail
if [ -z "$region" ]; then
    echo "Error: FILEIT_REGION environment variable not set"
    exit 1
fi


# The project naming convention relies on a unique stem value, derived 
# from my tenant custom domain, so that resources are also uniquely named.
# Since I've create this with my own unique stem, you'll need to vary yours.
stem="${FILEIT_STEM}"
# if stem not set in env var, fail
if [ -z "$stem" ]; then
    echo "Error: FILEIT_STEM environment variable not set"
    exit 1
fi

# the storage account name, which must be globally unique across Azure
storage_name="${FILEIT_STORAGE}"
# if storage account name not set in env var, fail
if [ -z "$storage_name" ]; then
    echo "Error: FILEIT_STORAGE environment variable not set"
    exit 1
fi

# ------------------------------------------------------
# Resources used by all function apps
# ------------------------------------------------------

# service bus
bus_name="$stem-bus"
bus_group_name="rg-$bus_name"

# application insights
appinsights_name="$stem-appinsights"
appinsights_group_name="rg-$appinsights_name"

# database, server named above, here is the resource group name
database_group_name="rg-$database_name-database"

# storage account, named above, here is the resource group name
storage_group_name="rg-$stem-storage"

# key vault, not used, but just in case
keyvault_name="$stem-keyvault"
keyvault_group_name="rg-$keyvault_name"
# ------------------------------------------------------


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


# authenticating with the devops service principal
# and its certificate
login_azure(){
    echo "login_azure method in base.sh"
    # Check if logged into Azure
    if ! az account show >/dev/null 2>&1; then
        echo "Logging into Azure using service principal $devops_client_id"
        login=$(az login \
                --service-principal \
                -u $devops_client_id \
                -t $tenant_id \
                --certificate $devops_client_key_path)
        if [[ $? -ne 0 ]]; then
            echo "Failed to login, end script"
            exit 1
        fi
    fi
}

# logging out of azure, specifically the devops spn that was used to login
logout_azure(){
    az logout --username $devops_spn
}
# ------------------------------------------------------


# ------------------------------------------------------
# SERVICE BUS FUNCTIONS
# ------------------------------------------------------

# create a service bus queue, based on the queue name passed in
# and the resource group / namespace that the queue should be deployed into
create_queue(){
    p_queueName=$1

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_queueName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file $repo_home/cmeraz-fileit/scripts/templates/bus_queue.bicep \
        --parameters \
            namespace=$bus_name \
            name=$p_queueName
}


# create a service bus topic, based on the topic name passed in
# and the resource group / namespace that the topic should be deployed into
create_topic(){
    p_topicName=$1

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_topicName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file $repo_home/cmeraz-fileit/scripts/templates/bus_topic.bicep \
        --parameters \
            namespace=$bus_name \
            name=$p_topicName
}


# create a service bus topic subscription, based on the topic / subscription name passed in
# and the resource group / namespace that the subscription should be deployed into
create_topic_subscription(){
    p_topicName=$1
    p_subscriptionName=$2

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_subscriptionName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file $repo_home/cmeraz-fileit/scripts/templates/bus_subscription.bicep \
        --parameters \
            namespace=$bus_name \
            topicName=$p_topicName \
            subscriptionName=$p_subscriptionName
}

# ------------------------------------------------------


# ------------------------------------------------------
# FUNCTION APP FUNCTIONS
# ------------------------------------------------------

# create an event grid subscription, based on the 
# managed identity / function / container / topic
# that the event grid subscription should be deployed into.
# This must be executed after these resources are deployed.
create_eventgrid_subscription(){
    p_managedIdentityName=$1
    p_functionRGName=$2
    p_functionAppName=$3
    p_functionName=$4
    p_subscriptionName=$5
    p_containerName=$6
    p_eventGridTopicName=$7

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${p_subscriptionName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    # Build base parameters
    params=(
        --parameters
        subscriptionId=$sub_id
        storageRGName=$storage_group_name
        storageAccountName=$storage_name
        functionRGName=$p_functionRGName
        functionAppName=$p_functionAppName
        functionName=$p_functionName
        subscriptionName=$p_subscriptionName
        containerName=$p_containerName
        eventGridTopicName=$p_eventGridTopicName
        identityName=$p_managedIdentityName
    )

    az deployment sub create \
        --name $deployment_name \
        --location $region \
        --template-file $repo_home/cmeraz-fileit/scripts/templates/eventgrid_sub.bicep "${params[@]}"
}


# create a function app, based on the name ending passed in
# this will derive the function app / resource group / managed identity
# names from the stem + the name ending
create_func(){
    p_nameEnding=$1

    resource_name="fa-$stem-$p_nameEnding"
    resource_group_name="rg-$stem-$p_nameEnding"
    userName="mi-$stem-$p_nameEnding"

    # Derive a valid storage account name from the resource name: lowercase,
    # remove hyphens, and limit to 24 characters (Azure storage account rules).
    resourceStorageAccountName=$(echo "${resource_name}-storage" | tr '[:upper:]' '[:lower:]' | tr -d '-' | cut -c1-24)

    # Validate resourceStorageAccountName: must be 3-24 chars, lowercase letters and digits only
    if [[ -z "$resourceStorageAccountName" ]]; then
        echo "ERROR: resourceStorageAccountName is empty. Derived value from resource name: '${resource_name}-storage'"
        exit 1
    fi
    if [[ ! $resourceStorageAccountName =~ ^[a-z0-9]{3,24}$ ]]; then
        echo "ERROR: resourceStorageAccountName '$resourceStorageAccountName' is invalid. It must be 3-24 characters, lowercase letters and digits only."
        exit 1
    fi

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="$resource_group_name-$timestamp"
    echo "Deployment name: $deployment_name"

    az deployment sub create \
        --name $deployment_name \
        --location $region \
        --template-file $repo_home/cmeraz-fileit/scripts/templates/func_sub.bicep \
        --parameters \
            resourceName=$resource_name \
            resourceGroupName=$resource_group_name \
            resourceStorageAccountName=$resourceStorageAccountName \
            storageAccountName=$storage_name \
            storageAccountGroupName=$storage_group_name \
            stem=$stem \
            location=$region \
            deploymentName=$deployment_name \
            appInsightsName=$appinsights_name \
            appInsightsGroupName=$appinsights_group_name \
            sqlServerName=$sql_server_name \
            databaseName=$database_name \
            databaseGroupName=$database_group_name \
            busName=$bus_name \
            busGroupName=$bus_group_name \
            userName=$userName
}

# ------------------------------------------------------
