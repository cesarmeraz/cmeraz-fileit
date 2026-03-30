#!/bin/bash
. ~/repos/cmeraz-fileit/scripts/base.sh


echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

# Fetch all roles and format as "DisplayName:GUID"
# We use @tsv to make it easy to loop through
role_data=$(az role definition list --query "[].{name:name, roleName:roleName}" -o tsv)

# Declare an associative array (requires Bash 4+)
declare -A role_dict

# delete roles.txt if it exists
rm -f ~/repos/cmeraz-fileit/scripts/principals/roles.txt

# create a new roles.txt file with the role data
echo "$role_data" > ~/repos/cmeraz-fileit/scripts/principals/roles.txt

# printout the role data from file
while IFS=$'\t' read -r guid name; do
    echo "Role Name: $name, GUID: $guid"
done < ~/repos/cmeraz-fileit/scripts/principals/roles.txt


logout_azure
echo "Done"
exit 0