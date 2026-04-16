#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

timestamp=$(date +"%Y%m%d-%H%M%S")
deployment_name="user-${timestamp}"
echo "Deployment name: $deployment_name"

login_azure


az deployment sub create \
    --name $deployment_name \
    --location $region \
    --template-file ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/templates/user_sub.bicep 

logout_azure
echo "Done"
exit 0