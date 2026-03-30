#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version


login_azure

create_topic_subscription 'api-add-topic' 'api-add-simple-sub'

logout_azure
echo "Done"
exit 0