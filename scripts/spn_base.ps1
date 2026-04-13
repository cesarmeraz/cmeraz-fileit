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

# The name of a devops service principal dedicated to 
# running deployment scripts from this project or from
# pipelines like Jenkins
$devops_spn = $env:FILEIT_DEVOPS_SERVICE_PRINCIPAL
# if devops service principal name not set in env var, fail
if ([string]::IsNullOrEmpty($devops_spn)) {
    Write-Host "Error: FILEIT_DEVOPS_SERVICE_PRINCIPAL environment variable not set"
    exit 1
}


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
