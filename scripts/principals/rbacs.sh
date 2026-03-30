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

# Populate the dictionary
while IFS=$'\t' read -r guid name; do
    role_dict["$name"]="$guid"
done <<< "$role_data"

# --- Examples of how to use it ---

# Lookup specific roles
blob_contributor_id=${role_dict["Storage Blob Data Contributor"]}
event_grid_id=${role_dict["EventGrid EventSubscription Contributor"]}

echo "Storage Blob Data Contributor GUID: $blob_contributor_id"
echo "EventGrid Contributor GUID: $event_grid_id"

# Use it in a command
# az role assignment create --assignee <id> --role "$blob_contributor_id" --scope <scope>


simpleFAPrincipalId=$(az functionapp identity show \
  --name "$stem-simple" \
  --resource-group "rg-$stem-simple" \
  --query "principalId" \
  --output tsv)




logout_azure
echo "Done"
exit 0