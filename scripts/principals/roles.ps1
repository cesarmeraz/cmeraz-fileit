. "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/base.ps1"

Write-Host "PWD: $(Get-Location)"
Write-Host "Running $($MyInvocation.MyCommand.Name)"
az version
login_azure

# Fetch all roles and format as "name roleName"
$role_data = az role definition list --query "[].{name:name, roleName:roleName}" -o tsv

# delete roles.txt if it exists
$rolesFile = "$env:FILEIT_REPO_HOME/cmeraz-fileit/scripts/principals/roles.txt"
if (Test-Path $rolesFile) {
    Remove-Item $rolesFile
}

# create a new roles.txt file with the role data
$role_data | Out-File -FilePath $rolesFile -Encoding utf8

# printout the role data from file
Get-Content $rolesFile | ForEach-Object {
    $fields = $_ -split "`t"
    $guid = $fields[0]
    $name = $fields[1]
    Write-Host "Role Name: $name, GUID: $guid"
}

logout_azure
Write-Host "Done"
exit 0
