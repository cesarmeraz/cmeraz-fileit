# ------------------------------------------------------
# GENERAL AZURE SETTINGS AND LOCAL ENVIRONMENT VARIABLES
# ------------------------------------------------------
# Your subscription id in GUID form
$sub_id = $env:SUBSCRIPTION_ID
#if subscription id not set in env var, fail
if ([string]::IsNullOrEmpty($sub_id)) {
    Write-Host "Error: SUBSCRIPTION_ID environment variable not set"
    exit 1
}


# Your tenant id in GUID form
$tenant_id = $env:TENANT_ID
#if tenant id not set in env var, fail
if ([string]::IsNullOrEmpty($tenant_id)) {
    Write-Host "Error: TENANT_ID environment variable not set"
    exit 1
}


# The server name, not the fully qualified domain name
$sql_server_name = $env:AZURE_SQL_SERVER
#if sql server name not set in env var, fail
if ([string]::IsNullOrEmpty($sql_server_name)) {
    Write-Host "Error: AZURE_SQL_SERVER environment variable not set"
    exit 1
}



# The name of the database on the server, FileIt is mine
$database_name = $env:AZURE_SQL_DATABASE
#if database name not set in env var, fail
if ([string]::IsNullOrEmpty($database_name)) {
    Write-Host "Error: AZURE_SQL_DATABASE environment variable not set"
    exit 1
}

# The root of the repo on the local dev box / build agent
$repo_home = $env:FILEIT_REPO_HOME
#if repo home not set in env var, fail
if ([string]::IsNullOrEmpty($repo_home)) {
    Write-Host "Error: FILEIT_REPO_HOME environment variable not set"
    exit 1
}

# ------------------------------------------------------



# ------------------------------------------------------
# SOLUTION ENVIRONMENT VARIABLES
# ------------------------------------------------------

# The full path to the parent folder containing
# child folders that contain the service principal
# certificates
$cert_parent_path = $env:CERT_PARENT_PATH

# if cert parent path not set in env var, fail
if ([string]::IsNullOrEmpty($cert_parent_path)) {
    Write-Host "Error: CERT_PARENT_PATH environment variable not set"
    exit 1
}


# The devops service principal id, for authentication
# In Entra ID, this is the Application (client) ID of the SPN
# found in App registrations
$devops_client_id = $env:FILEIT_DEVOPS_CLIENT_ID
# if devops client id not set in env var, fail
if ([string]::IsNullOrEmpty($devops_client_id)) {
    Write-Host "Error: FILEIT_DEVOPS_CLIENT_ID environment variable not set"
    exit 1
}


# The name of a devops service principal dedicated to 
# running deployment scripts from this project or from
# pipelines like Jenkins
$devops_spn = $env:FILEIT_DEVOPS_SERVICE_PRINCIPAL
# if devops service principal name not set in env var, fail
if ([string]::IsNullOrEmpty($devops_spn)) {
    Write-Host "Error: FILEIT_DEVOPS_SERVICE_PRINCIPAL environment variable not set"
    exit 1
}

# The location to the devops certificate, for authentication
$devops_client_key_path = "$cert_parent_path/$devops_spn/$devops_spn.pem"

# A single region for all resource groups in this project
# resources inherit their location value from their resource groups
$region = $env:FILEIT_REGION
# if region not set in env var, fail
if ([string]::IsNullOrEmpty($region)) {
    Write-Host "Error: FILEIT_REGION environment variable not set"
    exit 1
}


# The project naming convention relies on a unique stem value, derived 
# from my tenant custom domain, so that resources are also uniquely named.
# Since I've create this with my own unique stem, you'll need to vary yours.
$stem = $env:FILEIT_STEM
# if stem not set in env var, fail
if ([string]::IsNullOrEmpty($stem)) {
    Write-Host "Error: FILEIT_STEM environment variable not set"
    exit 1
}

# the storage account name, which must be globally unique across Azure
$storage_name = $env:FILEIT_STORAGE
# if storage account name not set in env var, fail
if ([string]::IsNullOrEmpty($storage_name)) {
    Write-Host "Error: FILEIT_STORAGE environment variable not set"
    exit 1
}

# ------------------------------------------------------
# Resources used by all function apps
# ------------------------------------------------------

# service bus
$bus_name = "$stem-bus"
$bus_group_name = "rg-$bus_name"

# application insights
$appinsights_name = "$stem-appinsights"
$appinsights_group_name = "rg-$appinsights_name"

# database, server named above, here is the resource group name
$database_group_name = "rg-$database_name-database"

# storage account, named above, here is the resource group name
$storage_group_name = "rg-$stem-storage"

# key vault, not used, but just in case
$keyvault_name = "$stem-keyvault"
$keyvault_group_name = "rg-$keyvault_name"
# ------------------------------------------------------


# ------------------------------------------------------
# SERVICE PRINCIPAL FUNCTIONS
# ------------------------------------------------------

# create a service principal with a cert for devops operations
function create_spn {
    param(
        [string]$p_servicePrincipalName,
        [string]$p_role
    )

    # "service principal name: $p_servicePrincipalName"
    if ([string]::IsNullOrEmpty($p_servicePrincipalName)) {
        Write-Host "Missing service principal name"
        exit 1
    }

    # Set variables
    $scope = "/subscriptions/$sub_id"
    $certPath = "$cert_parent_path/$p_servicePrincipalName/"

    New-Item -ItemType Directory -Force -Path $certPath | Out-Null
    Set-Location $certPath

    # "Delete pem files from $certPath"
    Remove-Item "$certPath/*.pem" -Force -ErrorAction SilentlyContinue

    # "List existing service principal records with same displayName"
    $existingAppId = (az ad app list `
                    --filter "displayName eq '$p_servicePrincipalName'" `
                    | ConvertFrom-Json)[0].appId
    if (-not [string]::IsNullOrEmpty($existingAppId)) {
        az ad app delete --id $existingAppId
    }

    # "Creating SP for RBAC, with certificate"
    $fileWithCertAndPrivateKey = (az ad sp create-for-rbac `
                            --name $p_servicePrincipalName `
                            --role $p_role `
                            --scopes $scope `
                            --create-cert | ConvertFrom-Json).fileWithCertAndPrivateKey

    # "rename the pem"
    Move-Item -Force $fileWithCertAndPrivateKey "$p_servicePrincipalName.pem"


    $sp_appId = az ad sp list `
                --display-name $p_servicePrincipalName `
                --output tsv --query "[].appId"
    Write-Output $sp_appId # echo the return value
}


# authenticating with the devops service principal
# and its certificate
function login_azure {
    Write-Host "login_azure method in base.ps1"
    # Check if logged into Azure
    az account show 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Logging into Azure using service principal $devops_client_id"
        az login `
            --service-principal `
            -u $devops_client_id `
            -t $tenant_id `
            --certificate $devops_client_key_path | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to login, end script"
            exit 1
        }
    }
}

# logging out of azure, specifically the devops spn that was used to login
function logout_azure {
    az logout --username $devops_spn
}
# ------------------------------------------------------


# ------------------------------------------------------
# SERVICE BUS FUNCTIONS
# ------------------------------------------------------

# create a service bus queue, based on the queue name passed in
# and the resource group / namespace that the queue should be deployed into
function create_queue {
    param(
        [string]$p_queueName
    )

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $deployment_name = "$bus_group_name-$p_queueName-$timestamp"
    Write-Host "Deployment name: $deployment_name"

    az deployment group create `
        --resource-group $bus_group_name `
        --name $deployment_name `
        --template-file "$repo_home/cmeraz-fileit/scripts/templates/bus_queue.bicep" `
        --parameters `
            namespace=$bus_name `
            name=$p_queueName
}


# create a service bus topic, based on the topic name passed in
# and the resource group / namespace that the topic should be deployed into
function create_topic {
    param(
        [string]$p_topicName
    )

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $deployment_name = "$bus_group_name-$p_topicName-$timestamp"
    Write-Host "Deployment name: $deployment_name"

    az deployment group create `
        --resource-group $bus_group_name `
        --name $deployment_name `
        --template-file "$repo_home/cmeraz-fileit/scripts/templates/bus_topic.bicep" `
        --parameters `
            namespace=$bus_name `
            name=$p_topicName
}


# create a service bus topic subscription, based on the topic / subscription name passed in
# and the resource group / namespace that the subscription should be deployed into
function create_topic_subscription {
    param(
        [string]$p_topicName,
        [string]$p_subscriptionName
    )

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $deployment_name = "$bus_group_name-$p_subscriptionName-$timestamp"
    Write-Host "Deployment name: $deployment_name"

    az deployment group create `
        --resource-group $bus_group_name `
        --name $deployment_name `
        --template-file "$repo_home/cmeraz-fileit/scripts/templates/bus_subscription.bicep" `
        --parameters `
            namespace=$bus_name `
            topicName=$p_topicName `
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
function create_eventgrid_subscription {
    param(
        [string]$p_managedIdentityName,
        [string]$p_functionRGName,
        [string]$p_functionAppName,
        [string]$p_functionName,
        [string]$p_subscriptionName,
        [string]$p_containerName,
        [string]$p_eventGridTopicName
    )

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $deployment_name = "$p_subscriptionName-$timestamp"
    Write-Host "Deployment name: $deployment_name"

    az deployment sub create `
        --name $deployment_name `
        --location $region `
        --template-file "$repo_home/cmeraz-fileit/scripts/templates/eventgrid_sub.bicep" `
        --parameters `
            subscriptionId=$sub_id `
            storageRGName=$storage_group_name `
            storageAccountName=$storage_name `
            functionRGName=$p_functionRGName `
            functionAppName=$p_functionAppName `
            functionName=$p_functionName `
            subscriptionName=$p_subscriptionName `
            containerName=$p_containerName `
            eventGridTopicName=$p_eventGridTopicName `
            identityName=$p_managedIdentityName
}


# create a function app, based on the name ending passed in
# this will derive the function app / resource group / managed identity
# names from the stem + the name ending
function create_func {
    param(
        [string]$p_nameEnding
    )

    $resource_name = "fa-$stem-$p_nameEnding"
    $resource_group_name = "rg-$stem-$p_nameEnding"
    $userName = "mi-$stem-$p_nameEnding"

    # Derive a valid storage account name from the resource name: lowercase,
    # remove hyphens, and limit to 24 characters (Azure storage account rules).
    $resourceStorageAccountName = ("$resource_name-storage" -replace '-','').ToLower()
    if ($resourceStorageAccountName.Length -gt 24) {
        $resourceStorageAccountName = $resourceStorageAccountName.Substring(0, 24)
    }

    # Validate resourceStorageAccountName: must be 3-24 chars, lowercase letters and digits only
    if ([string]::IsNullOrEmpty($resourceStorageAccountName)) {
        Write-Host "ERROR: resourceStorageAccountName is empty. Derived value from resource name: '$resource_name-storage'"
        exit 1
    }
    if ($resourceStorageAccountName -notmatch '^[a-z0-9]{3,24}$') {
        Write-Host "ERROR: resourceStorageAccountName '$resourceStorageAccountName' is invalid. It must be 3-24 characters, lowercase letters and digits only."
        exit 1
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $deployment_name = "$resource_group_name-$timestamp"
    Write-Host "Deployment name: $deployment_name"

    az deployment sub create `
        --name $deployment_name `
        --location $region `
        --template-file "$repo_home/cmeraz-fileit/scripts/templates/func_sub.bicep" `
        --parameters `
            resourceName=$resource_name `
            resourceGroupName=$resource_group_name `
            resourceStorageAccountName=$resourceStorageAccountName `
            storageAccountName=$storage_name `
            storageAccountGroupName=$storage_group_name `
            stem=$stem `
            location=$region `
            deploymentName=$deployment_name `
            appInsightsName=$appinsights_name `
            appInsightsGroupName=$appinsights_group_name `
            sqlServerName=$sql_server_name `
            databaseName=$database_name `
            databaseGroupName=$database_group_name `
            busName=$bus_name `
            busGroupName=$bus_group_name `
            userName=$userName
}

# ------------------------------------------------------
