#!/usr/bin/env bash

. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
# login_azure
az login

# accessToken=$(az account get-access-token --resource https://database.windows.net/ --query accessToken --output tsv)
create_db_user "fileit-simple"

az logout
# logout_azure
echo "Done"
exit 0