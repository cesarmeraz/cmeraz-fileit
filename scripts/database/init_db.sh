#!/bin/bash

# --- Configuration Variables ---

RESOURCE_GROUP_NAME="rg-meraz-database"
SQL_SERVER_NAME="${AZURE_SQL_SERVER}" # The logical SQL server name (e.g., myserver), not the full FQDN
DATABASE_NAME="${AZURE_SQL_DATABASE}"

# The name for the SQL user created from the managed identity; typically the same as the service name using it
SQL_IDENTITY_USER_NAME="fileit-simple" 
# -------------------------------
az login
echo "Enabling System-Assigned Managed Identity for SQL Server: $SQL_SERVER_NAME"
# Enable the system-assigned managed identity on the SQL logical server
# az sql server update --name "$SQL_SERVER_NAME" --resource-group "$RESOURCE_GROUP_NAME" --identity-type SystemAssigned

# echo "Retrieving the Principal ID of the newly created System-Assigned Managed Identity..."
# Retrieve the Principal ID (Object ID) of the created identity
# PRINCIPAL_ID=$(az sql server show --name "$SQL_SERVER_NAME" --resource-group "$RESOURCE_GROUP_NAME" --query identity.principalId --output tsv)

# if [ -z "$PRINCIPAL_ID" ]; then
#     echo "Failed to retrieve Principal ID. Exiting."
#     exit 1
# fi

# echo "Principal ID: $PRINCIPAL_ID"

echo "Creating the managed identity user in the SQL database using sqlcmd..."
# Use sqlcmd to connect with the Microsoft Entra admin credentials and create the user
# Ensure sqlcmd is installed and the admin credentials are valid.
# Note: For security, use environment variables for passwords instead of passing them as direct arguments.

# For a system-assigned identity, the name should match the service principal name in Azure AD.
# The user in SQL DB is created FROM EXTERNAL PROVIDER
CREATE_USER_QUERY="CREATE USER [${SQL_IDENTITY_USER_NAME}] FROM EXTERNAL PROVIDER;"

# Connect using Active Directory Password authentication with the AD admin account
# The -G flag is for Azure AD authentication, but here we use -U and -P to provide admin credentials
# to perform the CREATE USER operation.
sqlcmd -S tcp:"${SQL_SERVER_NAME}".database.windows.net,1433 -d "${DATABASE_NAME}" -G -Q "${CREATE_USER_QUERY}" -l 120

if [ $? -eq 0 ]; then
    echo "Successfully created SQL user [${SQL_IDENTITY_USER_NAME}] from System-Assigned Managed Identity."
    echo "Don't forget to grant specific permissions to the user within the database, e.g., db_datareader."
else
    echo "Failed to create SQL user. Please check your credentials and permissions."
fi

az logout
echo "Done"
exit 0