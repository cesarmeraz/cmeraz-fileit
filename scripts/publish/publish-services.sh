#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

resource_name="fa-$stem-services"
resource_group_name="rg-$stem-services"

cd ~/repos/cmeraz-fileit/FileIt.Module.Services
dotnet publish --configuration Release

az functionapp deployment source config-zip \
  -g $resource_group_name \
  -n $resource_name \
  --src ./FileIt.Module.Services.Host/bin/Release/net10.0/FileIt.Module.Services.Host.zip

logout_azure