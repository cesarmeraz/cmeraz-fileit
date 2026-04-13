#!/usr/bin/env pwsh

# Upload a file to Azurite blob storage container "simple-source" using Azure CLI.
# Usage: ./create-containers.ps1
$ErrorActionPreference = "Stop"

# Note: In an interactive environment you should call the Azure best-practices helper
# before generating or running Azure-related code (get_bestpractices resource=general action=code-generation).

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "ERROR: Azure CLI (az) not found in PATH."
    exit 2
}

# Define the container name from filename without extension
$CONTAINER_SOURCE = "simple-source"
$CONTAINER_WORKING = "simple-working"
$CONTAINER_FINAL = "simple-final"
$CONN = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
# $CONN = $env:AZURE_STORAGE_CONNECTION_STRING

# USE AZURE_STORAGE_CONNECTION_STRING environment variable.
Write-Host "Using AZURE_STORAGE_CONNECTION_STRING: $($CONN ?? '<not set>')"

# Ensure containers exist (create if missing)
Write-Host "Ensuring container '$CONTAINER_SOURCE' exists..."
az storage container create `
    --name $CONTAINER_SOURCE `
    --connection-string $CONN

Write-Host "Ensuring container '$CONTAINER_WORKING' exists..."
az storage container create `
    --name $CONTAINER_WORKING `
    --connection-string $CONN

Write-Host "Ensuring container '$CONTAINER_FINAL' exists..."
az storage container create `
    --name $CONTAINER_FINAL `
    --connection-string $CONN

Write-Host "Ensuring container '$CONTAINER_FINAL' exists..."
az storage container create `
    --name $CONTAINER_FINAL `
    --connection-string $CONN `
    --only-show-errors | Out-Null


# Create a test file to upload
$TIMESTAMP = Get-Date -Format "yyyyMMddHHmmss"
$FILENAME = "test_${TIMESTAMP}.txt"
"The current working directory is: $(Get-Location)" | Out-File -Append -FilePath $FILENAME

$BLOB_NAME = [System.IO.Path]::GetFileName($FILENAME)
Write-Host "Uploading test file '$FILENAME' as blob '$BLOB_NAME' to container '$CONTAINER_SOURCE'..."
az storage blob upload `
    --container-name $CONTAINER_SOURCE `
    --file $FILENAME `
    --name $BLOB_NAME `
    --connection-string $CONN `
    --overwrite `
    --only-show-errors

Write-Host "Upload complete: container='$CONTAINER_SOURCE' blob='$BLOB_NAME'"
exit 0
