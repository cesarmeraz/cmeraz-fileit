#!/usr/bin/env bash

# Upload a file to Azurite blob storage container "simple-source" using Azure CLI.
# Usage: ./simple.sh /path/to/file
set -euo pipefail

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


DIRECTORY="$(pwd)/scripts/crons_local"

if [ ! -d "$DIRECTORY" ]; then
    echo "ERROR: Directory not found: $DIRECTORY" >&2
    exit 3
fi

# Prefer AZURE_STORAGE_CONNECTION_STRING environment variable.
# If not set, you can start Azurite with defaults and set this accordingly.
# Example Azurite connection string (common default):
# DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDl3qEXAMPLE==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
CONN="${AZURE_STORAGE_CONNECTION_STRING:-}"

if [ -z "$CONN" ]; then
    echo "WARNING: AZURE_STORAGE_CONNECTION_STRING not set. Attempting common Azurite default connection string." >&2
    CONN="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDl3rEXAMPLEKEY==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
    echo "If this fails, set AZURE_STORAGE_CONNECTION_STRING to your Azurite connection string." >&2
fi

# Ensure container exists (creates if missing)
echo "Ensuring container '$CONTAINER_SOURCE' exists..."
az storage container create \
    --name "$CONTAINER_SOURCE" \
    --connection-string "$CONN" \
    --only-show-errors >/dev/null

echo "Ensuring container '$CONTAINER_WORKING' exists..."
az storage container create \
    --name "$CONTAINER_WORKING" \
    --connection-string "$CONN" \
    --only-show-errors >/dev/null

echo "Ensuring container '$CONTAINER_FINAL' exists..."
az storage container create \
    --name "$CONTAINER_FINAL" \
    --connection-string "$CONN" \
    --only-show-errors >/dev/null

# # Create a test file to upload
# TIMESTAMP=$(date +%Y%m%d%H%M%S)
# FILENAME="test_${TIMESTAMP}.txt"
# echo "The current working directory is: $(pwd)" >> "$FILENAME"


# BLOB_NAME="$(basename "$FILENAME")"

# echo "Uploading '$FILENAME' as blob '$BLOB_NAME' to container '$CONTAINER_NAME'..."
# az storage blob upload \
#     --container-name "$CONTAINER_NAME" \
#     --file "$FILENAME" \
#     --name "$BLOB_NAME" \
#     --connection-string "$CONN" \
#     --overwrite \
#     --only-show-errors

# echo "Upload complete: container='$CONTAINER_NAME' blob='$BLOB_NAME'"
exit 0