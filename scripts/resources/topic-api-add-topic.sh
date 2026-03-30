#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version


login_azure

create_topic 'api-add-topic'

logout_azure
echo "Done"
exit 0