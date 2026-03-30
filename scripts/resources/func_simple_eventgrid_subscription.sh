#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

login_azure

create_eventgrid_subscription \
    "rg-$stem-simple" \
    "$stem-simple" \
    "SimpleWatcher" \
    "simple-ingest-sub" \
    "simple-source" \
    "simple-ingest-sub-topic"

logout_azure
echo "Done"
exit 0