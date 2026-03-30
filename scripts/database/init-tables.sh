#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
# login_azure


# accessToken=$(az account get-access-token --resource https://database.windows.net/ --query accessToken --output tsv)
sqlcmd -S meraz.database.windows.net -d FileIt -G -Q "SELECT @@VERSION"



# sqlcmd -S meraz.database.windows.net -d FileIt -G -I -i databases/fileit/Tables/ApiLog.sql
# sqlcmd -S meraz.database.windows.net -d FileIt -G -I -i databases/fileit/Tables/CommonLog.sql
# sqlcmd -S meraz.database.windows.net -d FileIt -G -I -i databases/fileit/Tables/SimpleRequestLog.sql

# logout_azure
echo "Done"
exit 0