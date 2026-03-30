#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

resource_name="$stem-common"
resource_group_name="rg-$resource_name"

cd ~/repos/cmeraz-fileit/FileIt.Common
dotnet publish --configuration Release

az functionapp deployment source config-zip \
  -g $resource_group_name \
  -n $resource_name \
  --src ./FileIt.Common/bin/Release/net8.0/FileIt_Common.zip

logout_azure