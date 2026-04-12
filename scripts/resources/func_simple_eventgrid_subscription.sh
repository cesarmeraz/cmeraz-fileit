#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version

login_azure

nameEnding="simple"

create_eventgrid_subscription \
    "mi-$stem-$nameEnding" \
    "rg-$stem-$nameEnding" \
    "fa-$stem-$nameEnding" \
    "SimpleWatcher" \
    "$nameEnding-ingest-sub" \
    "$nameEnding-source" \
    "$nameEnding-ingest-sub-topic"

logout_azure
echo "Done"
exit 0