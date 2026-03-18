#!/bin/bash
. scripts/base.sh

# This script creates a service principal with 
# certificate credentials as the deployment
# account. Save the principalId output in your 
# environment variable DEVOPS_CLIENT_ID, e.g. launch.json.
# Log in as owner to create this spn.

# Bash script
echo "Running $0 script"

echo "FILEIT_DEVOPS_CLIENT_ID: ${FILEIT_DEVOPS_CLIENT_ID}"

if [ -z "$devops_client_id" ]; then
    echo "Error: devops_client_id is not set"
    exit 1
fi

if [ -z "$sub_id" ]; then
    echo "Error: sub_id is not set"
    exit 1
fi
