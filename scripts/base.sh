
#!/usr/bin/env bash


# The full path to the parent folder containing
# child folders that contain the service principal
# certificates
# Stored in environment variables
cert_parent_path="${CERT_PARENT_PATH}"

# The name of a devops service principal dedicated to 
# running deployment scripts from this project or from
# pipelines like Jenkins
devops_spn=$FILEIT_DEVOPS_SERVICE_PRINCIPAL

# The devops service principal id, for authentication
devops_client_id="${FILEIT_DEVOPS_CLIENT_ID}"

# The location to the devops certificate, for authentication
devops_client_key_path="$cert_parent_path/$devops_spn/$devops_spn.pem"

# Your subscription id in GUID form
sub_id="${SUBSCRIPTION_ID}"

# Your tenant id in GUID form
tenant_id="${TENANT_ID}"

# A single region for all resource groups in this project
# resources inherit their location value from their resource groups
region="${FILEIT_REGION}"

# The project naming convention relies on a unique stem value, derived 
# from my tenant custom domain, so that resources are also uniquely named.
stem="${FILEIT_STEM}"

# Below are -most- of the resource names, which use the stem value in 
# their construction

database_name="${AZURE_SQL_DATABASE}"
sql_server_name="${AZURE_SQL_SERVER}"
storage_name="cmerazfileitstorage"
keyvault_name="$stem-keyvault"
bus_name="$stem-bus"
appinsights_name="$stem-appinsights"

# create resource group names
database_group_name="rg-meraz-database"
keyvault_group_name="rg-$keyvault_name"
storage_group_name="rg-$stem-storage"
bus_group_name="rg-$bus_name"
appinsights_group_name="rg-$appinsights_name"

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
logout_azure(){
    az logout --username $devops_spn
}

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

create_queue(){
    p_queueName=$1

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_queueName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    # List queues and query for the specific name, using tsv output for easy parsing
    QUEUE_COUNT=$(az servicebus queue list \
        --resource-group $bus_group_name \
        --namespace-name $bus_name \
        --query "[?name=='$p_queueName'] | length(@)" \
        --output tsv)

    if [ "$QUEUE_COUNT" -gt 0 ]; then
        echo "Queue '$p_queueName' found. It already exists."
        az servicebus queue delete \
            --resource-group $bus_group_name \
            --namespace-name $bus_name \
            --name $p_queueName
    else
        echo "Queue '$p_queueName' not found. It does not exist."
    fi


    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file scripts/templates/bus_queue.bicep \
        --parameters \
            namespace=$bus_name \
            name=$p_queueName

}

create_topic(){
    p_topicName=$1

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_topicName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    # List topics and query for the specific name, using tsv output for easy parsing
    TOPIC_COUNT=$(az servicebus topic list \
        --resource-group $bus_group_name \
        --namespace-name $bus_name \
        --query "[?name=='$p_topicName'] | length(@)" \
        --output tsv)

    if [ "$TOPIC_COUNT" -gt 0 ]; then
        echo "Topic '$p_topicName' found. It already exists."
        az servicebus topic delete \
            --resource-group $bus_group_name \
            --namespace-name $bus_name \
            --name $p_topicName
    else
        echo "Topic '$p_topicName' not found. It does not exist."
    fi


    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file scripts/templates/bus_topic.bicep \
        --parameters \
            namespace=$bus_name \
            name=$p_topicName
}

create_topic_subscription(){
    p_topicName=$1
    p_subscriptionName=$2

    timestamp=$(date +"%Y%m%d-%H%M%S")
    deployment_name="${bus_group_name}-${p_subscriptionName}-${timestamp}"
    echo "Deployment name: $deployment_name"

    # List subscriptions and query for the specific name, using tsv output for easy parsing
    SUBSCRIPTION_COUNT=$(az servicebus topic subscription list \
        --resource-group $bus_group_name \
        --namespace-name $bus_name \
        --topic-name $p_topicName \
        --query "[?name=='$p_subscriptionName'] | length(@)" \
        --output tsv)

    if [ "$SUBSCRIPTION_COUNT" -gt 0 ]; then
        echo "Subscription '$p_subscriptionName' found. It already exists."
        az servicebus topic subscription delete \
            --resource-group $bus_group_name \
            --namespace-name $bus_name \
            --topic-name $p_topicName \
            --name $p_subscriptionName
    else
        echo "Subscription '$p_subscriptionName' not found. It does not exist."
    fi


    az deployment group create \
        --resource-group $bus_group_name \
        --name $deployment_name \
        --template-file scripts/templates/bus_subscription.bicep \
        --parameters \
            namespace=$bus_name \
            topicName=$p_topicName \
            subscriptionName=$p_subscriptionName
}

create_eventgrid_subscription(){
    p_functionRGName=$1
    p_functionAppName=$2
    p_functionName=$3
    p_subscriptionName=$4
    p_containerName=$5
    p_eventGridTopicName=$6

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
    )

    # Derive user-assigned identity name (created as mi-<functionAppName> during function creation)
    userIdentityName="mi-${p_functionAppName}"

    # Attempt to read the principalId of the user-assigned identity
    principalId=$(az identity show --resource-group "$p_functionRGName" --name "$userIdentityName" --query principalId -o tsv 2>/dev/null || true)
    if [[ -z "$principalId" ]]; then
        echo "Warning: could not find user-assigned identity '$userIdentityName' in RG '$p_functionRGName'. Proceeding without principalId parameter."
    else
        echo "Found principalId for identity '$userIdentityName': $principalId"
        params+=( principalId=$principalId )
    fi

    az deployment sub create \
        --name $deployment_name \
        --location $region \
        --template-file scripts/templates/eventgrid_sub.bicep "${params[@]}"
}

create_func(){
    p_nameEnding=$1

    resource_name="$stem-$p_nameEnding"
    resource_group_name="rg-$resource_name"
    userName="mi-$stem-$p_nameEnding"

    # TODO: create parameter to optionally delete existing resource group, default to false, and if true, delete the resource group before creating the function app. This is useful for development iterations, but should be used with caution to avoid unintended deletions.
    # if [[ $(az group exists --name $resource_group_name) == "true" ]]; then
    #     echo "Deleting $resource_group_name"
    #     az group delete --name $resource_group_name --yes
    # fi

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
        --template-file scripts/templates/func_sub.bicep \
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

create_db_user(){
    p_userName=$1

    sqlcmd -S $sql_server_name.database.windows.net -d $database_name -G -Q "CREATE USER [$p_userName] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [$p_userName]; ALTER ROLE db_datawriter ADD MEMBER [$p_userName]; GO"
}