# set current working directory
#!/bin/bash

. ./scripts/base.sh

echo "testing simple local "
current_date_time="`date +%Y%m%d%H%M%S`"
echo "$current_date_time : starting simple local test" > ${ingest_path}/local/simple/simpletest.txt