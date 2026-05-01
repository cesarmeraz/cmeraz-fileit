#!/usr/bin/env bash
set -euo pipefail

. ${FILEIT_REPO_HOME}/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

resource_name="fa-$stem-simple"
resource_group_name="rg-$stem-simple"
project_path="./FileIt.Module.SimpleFlow.Host/FileIt.Module.SimpleFlow.Host.csproj"
zip_path="./FileIt.Module.SimpleFlow.Host/bin/Release/net10.0/FileIt.Module.SimpleFlow.Host.zip"
publish_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$publish_dir"
}
trap cleanup EXIT


cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.SimpleFlow
dotnet clean "$project_path" --configuration Release
dotnet publish "$project_path" --configuration Release --output "$publish_dir"

if ! command -v zip >/dev/null 2>&1; then
  echo "ERROR: zip command not found. Please install zip to package deployment artifacts."
  exit 1
fi

if [ ! -f "$publish_dir/FileIt.Module.SimpleFlow.Host.deps.json" ]; then
  echo "ERROR: Publish output is missing FileIt.Module.SimpleFlow.Host.deps.json"
  exit 1
fi

rm -f "$zip_path"
(
  cd "$publish_dir"
  zip -qr "$OLDPWD/$zip_path" .
)

if [ ! -s "$zip_path" ]; then
  echo "ERROR: Deployment zip was not created correctly: $zip_path"
  exit 1
fi

echo "Deploying fresh package: $zip_path"

az functionapp deployment source config-zip \
  -g "$resource_group_name" \
  -n "$resource_name" \
  --src "$zip_path"

echo "Restarting function app: $resource_name"
az functionapp restart -g "$resource_group_name" -n "$resource_name"

echo "Indexed functions after deployment:"
az functionapp function list -g "$resource_group_name" -n "$resource_name" --query "[].name" -o tsv || true

logout_azure