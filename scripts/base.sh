#!/usr/bin/env bash


# The full path to the parent folder containing
# child folders that contain the service principal
# certificates
cert_parent_path="${CERT_PARENT_PATH}"

# The name of a tool service principal that you can
# use when experimenting with the tool_spn.sh script
azcopy_spn="${AZCOPY_SERVICE_PRINCIPAL}"

# The name of a tool service principal that you can
# use when experimenting with the tool_spn.sh script
tool_spn="${TOOL_SERVICE_PRINCIPAL}"

# The name of a test service principal that you can
# use when experimenting with the admin_spn.sh script
test_spn="${TEST_SERVICE_PRINCIPAL}"

# The name of a test service principal that you can
# use when experimenting with the admin_spn.sh script
web_spn="${WEB_SERVICE_PRINCIPAL}"

# The name of a devops service principal dedicated to 
# running deployment scripts from this project or from
# pipelines like Jenkins
devops_spn="${DEVOPS_SERVICE_PRINCIPAL}"

# The devops service principal id, for authentication
azcopy_client_id="${AZCOPY_CLIENT_ID}"

# The devops service principal id, for authentication
tool_client_id="${TOOL_CLIENT_ID}"

# The devops service principal id, for authentication
devops_client_id="${DEVOPS_CLIENT_ID}"

# The web service principal id, for authentication
web_client_id="${WEB_CLIENT_ID}"

# The location to the devops certificate, for authentication
devops_client_key_path="$cert_parent_path/$devops_spn/$devops_spn.pem"

# Your subscription id in GUID form
sub_id="${SUBSCRIPTION_ID}"

# Your tenant id in GUID form
tenant_id="${TENANT_ID}"

# A single region for all resource groups in this project
# resources inherit their location value from their resource groups
region="${REGION}"

# I use a specific path in my home directory to stage files 
# for ingestion into Azure Blob Storage
ingest_path="${INGEST_PATH}"

# The project naming convention relies on a unique stem value, derived 
# from my tenant custom domain, so that resources are also uniquely named.
stem="${AZ_STEM}"

storage_url="${STORAGE_URL}"

storage_local_connection_string="${STORAGE_LOCAL_CONNECTION_STRING}"

# Below are -most- of the resource names, which use the stem value in 
# their construction

web_name="$stem-web"
apim_name="$stem-apim"
function_name="$stem-function"
storage_name+="cmerazfileitstorage"
keyvault_name="$stem-keyvault"
cosmos_name="$stem-cosmos"

# create resource group names
web_group_name="rg-$web_name"
apim_group_name="rg-$apim_name"
function_group_name="rg-$function_name"
keyvault_group_name="rg-$keyvault_name"
storage_group_name="rg-cmeraz-filesit-storage"
cosmos_group_name="rg-$cosmos_name"

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
