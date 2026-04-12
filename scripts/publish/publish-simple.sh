#!/usr/bin/env bash
. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

resource_name="fa-$stem-simple"
resource_group_name="rg-$stem-simple"


cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.SimpleFlow
dotnet publish --configuration Release

az functionapp deployment source config-zip \
  -g $resource_group_name \
  -n $resource_name \
  --src ./FileIt.Module.SimpleFlow.Host/bin/Release/net10.0/FileIt.Module.SimpleFlow.Host.zip

logout_azure