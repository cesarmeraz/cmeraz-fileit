#!/bin/bash
# this script appends the contents of source_cron.txt to the current user's crontab
. scripts/base.sh

# Verify the current working directory (optional)
echo "Current working directory: $(pwd)"

# Get the directory of the current script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Change to the script's directory
cd "$SCRIPT_DIR"

# Verify the current working directory (optional)
echo "Current working directory: $(pwd)"

#list directory contents before changes
echo "Directory contents:"
ls -l

# keep for when it is time to update crontab entries
# echo "write out current crontab"
# crontab -l > mycron

cp source_cron.txt temp_cron.txt

echo "Fill placeholders in cron file"
sed -i \
    -e "s|INGEST_PATH|${INGEST_PATH}|ig" \
    temp_cron.txt

# print out the new cron file for verification
echo "New cron file contents:"
cat temp_cron.txt

# this is ok if the crontab is empty to begin with
echo "install new cron file"
crontab temp_cron.txt

echo "Clean up temporary file"
rm temp_cron.txt


#list directory contents afterwards
echo "Directory contents:"
ls -l


#exit here for testing
exit 0