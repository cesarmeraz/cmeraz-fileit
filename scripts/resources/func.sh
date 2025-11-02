#!/usr/bin/env bash
    . scripts/base.sh

# This script depends on vnet creation

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

vnet_id=$(az network vnet list --resource-group "$vnet_group_name" --query "[?name=='${vnet_name}'].id" -o tsv)
echo "vnet_id: $vnet_id"

# array of third octets
declare -a arr=("10" "11" "12" "13")
# declare -a arr=("10")
for i in "${arr[@]}"
do
    this_function_group="rg-${stem}-function-${i}"
    this_function_name="${stem}-function-${i}"
    echo $this_function_group $this_function_name "${stem}storage${i}" $vnet_id "subnet-func-${i}"
    create_function $this_function_group $this_function_name "${stem}storage${i}" $vnet_id "subnet-func-${i}"
done