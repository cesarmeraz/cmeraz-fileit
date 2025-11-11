#!/usr/bin/env bash

# Upload a file to Azurite blob storage container "simple-source" using Azure CLI.
# Usage: ./simple.sh /path/to/file
set -euo pipefail

echo "Current working directory: $(pwd)"

# Note: In an interactive environment you should call the Azure best-practices helper
# before generating or running Azure-related code (get_bestpractices resource=general action=code-generation).

if ! command -v az >/dev/null 2>&1; then
    echo "ERROR: Azure CLI (az) not found in PATH." >&2
    exit 2
fi

# Define the container name from filename without extension
CONTAINER_SOURCE="simple-source"
CONTAINER_WORKING="simple-working"
CONTAINER_FINAL="simple-final"
CONN=$AZURE_STORAGE_CONNECTION_STRING

# USE AZURE_STORAGE_CONNECTION_STRING environment variable.
echo "Using AZURE_STORAGE_CONNECTION_STRING: ${CONN:-<not set>}"

# Ensure containers exist (create if missing)
echo "Ensuring container '$CONTAINER_SOURCE' exists..."
az storage container create \
    --name "$CONTAINER_SOURCE" \
    --connection-string "$CONN"

echo "Ensuring container '$CONTAINER_WORKING' exists..."
az storage container create \
    --name "$CONTAINER_WORKING" \
    --connection-string "$CONN" 

echo "Ensuring container '$CONTAINER_FINAL' exists..."
az storage container create \
    --name "$CONTAINER_FINAL" \
    --connection-string "$CONN" 

# Create a test file to upload
TIMESTAMP=$(date +%Y%m%d%H%M%S)
FILENAME="test_${TIMESTAMP}.txt"
echo "The current working directory is: $(pwd)" >> "$FILENAME"

BLOB_NAME="$(basename "$FILENAME")"
echo "Uploading test file '$FILENAME' as blob '$BLOB_NAME' to container '$CONTAINER_SOURCE'..."
az storage blob upload \
    --container-name "$CONTAINER_SOURCE" \
    --file "$FILENAME" \
    --name "$BLOB_NAME" \
    --connection-string "$CONN" \
    --overwrite \
    --only-show-errors

echo "Upload complete: container='$CONTAINER_SOURCE' blob='$BLOB_NAME'"
exit 0