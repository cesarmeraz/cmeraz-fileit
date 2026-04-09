#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

resource_name="$stem-simple"
resource_group_name="rg-$resource_name"


cd ~/repos/cmeraz-fileit/FileIt.SimpleFlow
dotnet publish --configuration Release

az functionapp deployment source config-zip \
  -g $resource_group_name \
  -n $resource_name \
  --src ./FileIt.SimpleFlow/bin/Release/net10.0/FileIt_SimpleFlow.zip

logout_azure