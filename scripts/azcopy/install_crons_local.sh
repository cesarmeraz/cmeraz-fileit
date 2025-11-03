#!/bin/bash
. scripts/base.sh

connection_string="${STORAGE_LOCAL_CONNECTION_STRING}"

# crons for local
for file in /workspaces/cmeraz-fileit/scripts/crons_local/*.sh; do
  # Check if the file actually exists and is a regular file
  if [ -f "$file" ]; then
    echo "Processing file: $file"

    # Setting execute permissions for $filename
    chmod a+x "${file}"

    filename=$(basename "$file")
    container_name="${filename%.sh}"

    az storage container create \
      --name $container_name \
      --connection-string $connection_string
  fi
done

