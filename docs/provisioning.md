# Provisioning Azure resources
All provisioning scripts are located in the /scripts/provisioning directory. These scripts will create the necessary Azure resources. Before running the provisioning scripts, make sure you have the necessary environment variables set, which are used to specify your Azure subscription, tenant, and other configuration details.

Script files incorporate a base script containing common functions and variables, which is located at /scripts/provisioning/provisioning_base.sh. This base script is sourced at the beginning of each provisioning script to ensure that all necessary functions and variables are available.

Scripts follow a predictable pattern of logging in as the devops service principal, setting the subscription context, and then creating the necessary resources. The scripts are idempotent, meaning you can run them multiple times without causing errors or creating duplicate resources. The scripts end by logging out.

## Environment Variables

On Ubuntu, I'm appending my environment variables to the bottom of the .bashrc file in my home directory, which gets loaded when I log in or when I execute the source ~/.bashrc command.

```bash
# Append provisioning environment variables to ~/.bashrc
cat >> ~/.bashrc <<'EOF'
# Azure provisioning environment variables
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_SQL_SERVER="your sql server name"
export AZURE_SQL_DATABASE="your sql database name"

# cmeraz-fileit variables
export CERT_PARENT_PATH="$HOME/certificates"
export FILEIT_DEVOPS_SERVICE_PRINCIPAL="your-service-principal-name"
export FILEIT_REGION="your-region"
export FILEIT_STEM="your unique naming stem"
export FILEIT_DEVOPS_CLIENT_ID="your-client-id"

EOF

# Reload ~/.bashrc
source ~/.bashrc
```

Of course there are ways to script this in Windows. The important thing is to have these environment variables set before you run the provisioning scripts, which will use them to create the necessary Azure resources.

For all the following scripts, execute at the solution root.

## DevOps Service Principal
All scripts are written to use a service principal for authentication, which is a best practice for automation. You can create a service principal in Azure AD and assign it the necessary permissions to manage resources in your subscription. The client ID of the service principal should be set in the FILEIT_DEVOPS_CLIENT_ID environment variable.

However, I've written a script to create this service principal with the Contributer and User Access Administrator roles, which are needed for provisioning resources and assigning permissions. You can run this script once to create the service principal and set the FILEIT_DEVOPS_CLIENT_ID environment variable.

```bash
# execute the /scripts/principals/create_devops_principal.sh script to create the service principal and set the FILEIT_DEVOPS_CLIENT_ID environment variable
bash ./scripts/principals/create_devops_principal.sh
```

## Database and Database Server
You may choose to use your own database server and database, or use a different naming convention for your database. Create this resource first if you don't have one already.

```bash
bash ./scripts/resources/database.sh
```

## Service Bus
Carrying on with the foundational services to this system, create the service bus next. 

```bash
bash ./scripts/resources/bus.sh
```

## Blob Storage
Next create the blob storage. Note that this is the shared storage account, used by the system. This is not to be confused with the storage accounts used for function app deployment. 

```bash
bash ./scripts/resources/storage.sh
```

## Application Insights
All function apps in the system share the same instance of Application Insights, so it must be created before any of those scripts execute.

```bash
bash ./scripts/resources/appinsights.sh
```

## FileIt_Common
Create the FileIt_Common function app. It includes the necessary RBAC role creations.

```bash
bash ./scripts/resources/func_common.sh
```

## FileIt_Simple
Then create the FileIt_Simple function app. It includes the necessary RBAC role creations. Additional workflows will be variations of this example.
```bash
bash ./scripts/resources/func_simple.sh
```
The above script creates the necessary function app resource but there may be additional resources unique to the workflow. For the simple workflow we need to create a subscription to an eventgridtrigger when a test file is dropped into the source container. Not all workflows will have this need and some may have multiple triggers, so this part is tailored to the workflow.

```bash
bash ./scripts/resources/func_simple_eventgrid_subscription.sh
```
