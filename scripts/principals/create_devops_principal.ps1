. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/spn_base.ps1"


# This script creates a service principal with 
# certificate credentials as the deployment
# account. Save the principalId output in your 
# environment variable DEVOPS_CLIENT_ID, e.g. launch.json.
# Log in as owner to create this spn.

# PowerShell script
Write-Host "Running $($MyInvocation.MyCommand.Name) script"

az login
$scope = "/subscriptions/$sub_id"
$devops_app_id = create_spn $devops_spn "Contributor"

$principalId = az ad sp list `
            --display-name $devops_spn `
            --output tsv --query "[].id"
Write-Host "principalId for ${devops_spn}: $principalId"
az role assignment create `
    --assignee-object-id $principalId `
    --assignee-principal-type "ServicePrincipal" `
    --role "Role Based Access Control Administrator" `
    --scope $scope

az role assignment create `
    --assignee-object-id $principalId `
    --assignee-principal-type "ServicePrincipal" `
    --role "Storage Blob Data Contributor" `
    --scope $scope

az logout

Write-Host @"
                "DEVOPS_CLIENT_ID": "$devops_app_id",
"@
