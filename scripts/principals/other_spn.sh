#!/bin/bash
. scripts/base.sh

# This script creates a service principal with 
# certificate credentials as the service account for
# the static web site. Save the principalId output in your 
# environment variable WEB_CLIENT_ID, e.g. launch.json.
# Log in as owner to create this spn.

# Bash script
echo "Running $0 script"

login_azure
azcopy_app_id=$(create_spn $azcopy_spn "Storage Blob Data Contributor")
test_app_id=$(create_spn $test_spn "Contributor")
tool_app_id=$(create_spn $tool_spn "Contributor")
web_app_id=$(create_spn $web_spn "Contributor")


az logout

cat <<- xx
    
                "AZCOPY_CLIENT_ID": "$azcopy_app_id",
                "TEST_CLIENT_ID": "$test_app_id",
                "TOOL_CLIENT_ID": "$tool_app_id",
                "WEB_CLIENT_ID": "$web_app_id",
xx
