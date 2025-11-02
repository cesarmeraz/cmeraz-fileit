#!/bin/bash
. scripts/base.sh

# Verify the current working directory (optional)
echo "Current working directory: $(pwd)"

# Get the directory of the current script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Change to the script's directory
cd "$SCRIPT_DIR"

# Verify the current working directory (optional)
echo "Current working directory: $(pwd)"
cert="${cert_parent_path}/$devops_spn/$devops_spn.pem"
echo "Using cert path: $cert"

storage_url="${STORAGE_URL}"
local_storage_url="${LOCAL_STORAGE_URL}"

az login \
    --service-principal \
    --certificate $cert \
    --tenant $tenant_id \
    --username $devops_client_id

# crons for azure
for file in scripts/crons/*.sh; do
  # Check if the file actually exists and is a regular file
  if [ -f "$file" ]; then
    echo "Processing file: $file"

    # Setting execute permissions for $filename
    chmod a+x "${file}"

    filename=$(basename "$file")
    container_name="${filename%.sh}"

    az storage container create \
        --name $container_name \
        --account-name $storage_name \
        --auth-mode login
  fi
done
az logout


# crons for local
for file in scripts/crons_local/*.sh; do
  # Check if the file actually exists and is a regular file
  if [ -f "$file" ]; then
    echo "Processing file: $file"

    # Setting execute permissions for $filename
    chmod a+x "${file}"
    filename=$(basename "$file")
    container_name="${filename%.sh}"

    az storage container create \
        --name $container_name \
        --account-name 'devstoreaccount1' \
        --auth-mode login
  fi
done
