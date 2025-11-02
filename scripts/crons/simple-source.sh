#!/bin/bash
# this script authenticates the azcopy tool with a certificate file 
# and uses the tool to move files from a staging directory to an 
# Azure Blob Storage container in Azure, which looks like this:
# https://yourstorageaccount.blob.core.windows.net/yourcontainername

# Load environment variables from .env file
source "$(dirname "$0")/.env"

# Define the container name from filename without extension
CONTAINER_NAME=$(basename "$0" .sh)

# Authenticate azcopy with a certificate file
azcopy login \
    --service-principal \
    --certificate-path $AZCOPY_CERT_PATH \
    --tenant-id $YOUR_TENANT_ID \
    --application-id $YOUR_APPLICATION_ID

# Use azcopy to move files from staging directory to Azure Blob Storage container
azcopy copy \
    "$INGEST_PATH/$CONTAINER_NAME/*" \
    "$STORAGE_URL/$CONTAINER_NAME" \
    --exclude-pattern "*.sh" 

# Log out azcopy
azcopy logout