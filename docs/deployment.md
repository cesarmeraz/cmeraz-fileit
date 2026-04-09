# Azure Deployment

## Flex Consumption tier Function Apps
Flex consumption function apps has a singular method for deployment, called OneDeploy. It involves two stages.

First, package the build output into a zip file. Any method will do, but I use MSBuild to do this in my csproj files, like this:
```xml
  <Target Name="Package" AfterTargets="Publish">
    <MakeDir Directories="$(MSBuildProjectDirectory)/bin/$(Configuration)/$(TargetFramework)/" />
    <ZipDirectory
      Overwrite="true"
      SourceDirectory="$(PublishDir)"
      DestinationFile="$(MSBuildProjectDirectory)/bin/$(Configuration)/$(TargetFramework)/$(ProjectName).zip"
    />
  </Target>
```
After a successful build, the output is zipped up and sits inside the output contents nested in bin. The build isn't timestamped, so it overwrites the previous package. This isn't what you'd do necessarily in an enterprise CI/CD pipeline, but for small projects, it is easy.

Then, use the CLI to upload the package to the function app's management storage account. It is the resource created in the bicep file when we provision the function app.

```bash
#!/usr/bin/env bash
. ~/repos/cmeraz-fileit/scripts/base.sh

echo "PWD: $(pwd)"
echo "Running $0"
az version
login_azure

resource_name="$stem-common"
resource_group_name="rg-$resource_name"

cd ~/repos/cmeraz-fileit/FileIt.Common
dotnet publish --configuration Release

az functionapp deployment source config-zip \
  -g $resource_group_name \
  -n $resource_name \
  --src ./FileIt.Common/bin/Release/net10.0/FileIt_Common.zip

logout_azure
```

This script imports our base script file, logs in using our devops service principal, packages the dotnet project using the Release configuration, and then calls the OneDeploy command, referencing the path to the package in the src parameter. Finally, it logs out of azure. This script is located under ./scripts/publish


## Database deployment
[DACPAC deployment](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/concepts/data-tier-applications/overview?view=sql-server-ver17#dacpac-operations) is really easy once you understand it. Object scripts are idempotent, which means that you can modify the object, like adding a column, and the rest of your object, including existing data, should stay in place. 

I've added two scripts under ./scripts/publish for deploying locally and deploying to Azure.
- ./scripts/publish/publish-database.sh
- ./scripts/publish/publish-database-local.sh

These have not been tested yet, but I'll keep them updated as we progress in the project.