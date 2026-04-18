# Extending this Project - New Function Apps

Here are some notes I took while creating and recreating the projects so that I could track all the steps. As you go through this, please edit in case I missed something. And let's keep this up-to-date if refactors cause a change to the script.

# The Module Unit

Something to preserve here is the idea that these Function Apps represent modules of implementation and deployment. They are not meant to communicate with other Function Apps directly; they communicate with the shared resources: database, service bus, blob storage and application insights. Therefore, this is a script for creating a set of new projects for one Function App, the module unit.

A module unit contains
1. the Function App project
2. a unit test project
3. an integration test project

> ***NOTE*** 
As of yet, I don't have a use for that last project but many of my colleagues write tests that go end to end. Rather than discourage those kinds of tests, I'd rather segregate them. 

The unit test project is important. It should not communicate with the shared resources. It should test target code in isolation and be safe for execution in any environment.

## Install the Test Framework

```bash
dotnet new install TUnit.Templates
```

## Create the Projects
```bash
mkdir FileIt.Module.ExampleFlow
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.ExampleFlow
func init FileIt.Module.ExampleFlow.Host --worker-runtime dotnet-isolated --target-framework net10.0
dotnet new classlib --name FileIt.Module.ExampleFlow.App --framework net10.0
dotnet new TUnit -n "FileIt.Module.ExampleFlow.Test" --framework net10.0
dotnet new TUnit -n "FileIt.Module.ExampleFlow.Integration" --framework net10.0
```

## Add Packages to the Function App
```bash
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.Host
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventGrid
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer
dotnet add package Microsoft.Azure.WebJobs
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Hosting.Abstractions
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.5
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.5
dotnet add package Serilog
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Formatting.Compact
dotnet add package Serilog.Settings.Configuration
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package System.Configuration.ConfigurationManager
```

## Add Project References to the Function App
```bash
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.Host
dotnet add reference ../../FileIt.Infrastructure/FileIt.Infrastructure/FileIt.Infrastructure.csproj 
dotnet add reference ../../FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.App/FileIt.Module.ExampleFlow.App.csproj 
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
```

## Add Project References to the Application
```bash
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.App
dotnet add reference ../../FileIt.Domain/FileIt.Domain/FileIt.Domain.csproj 
```

## Add Project References and Packages to the Test project
```bash
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.Test
dotnet add reference ../../FileIt.Infrastructure/FileIt.Infrastructure/FileIt.Infrastructure.csproj 
dotnet add reference ../../FileIt.Domain/FileIt.Domain/FileIt.Domain.csproj 
dotnet add reference ../FileIt.Module.ExampleFlow.App/FileIt.Module.ExampleFlow.App.csproj 
dotnet add package Microsoft.Extensions.Logging
dotnet add package coverlet.MTP
dotnet add package TUnit
dotnet add package TUnit.Mocks
dotnet add package TUnit.Mocks.Logging
```

## (Re)Create the Module Solution file
```bash
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/FileIt.Module.ExampleFlow
rm FileIt.Module.ExampleFlow.sln
dotnet new sln --name FileIt.Module.ExampleFlow
dotnet sln FileIt.Module.ExampleFlow.sln add ./FileIt.Module.ExampleFlow/FileIt_ExampleFlow.csproj
dotnet sln FileIt.Module.ExampleFlow.sln add ./FileIt.Module.ExampleFlow.App/FileIt.Module.ExampleFlow.App.csproj
dotnet sln FileIt.Module.ExampleFlow.sln add ./FileIt.Module.ExampleFlow.Test/FileIt.Module.ExampleFlow.Test.csproj
dotnet sln FileIt.Module.ExampleFlow.sln add ./FileIt.Module.ExampleFlow.Integration/FileIt.Module.ExampleFlow.Integration.csproj
```

## Add the Module to the main Solution
```bash
cd ${FILEIT_REPO_HOME}/cmeraz-fileit/
dotnet sln FileIt.All.sln add ./FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow/FileIt_ExampleFlow.csproj
dotnet sln FileIt.All.sln add ./FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.App/FileIt.Module.ExampleFlow.App.csproj
dotnet sln FileIt.All.sln add ./FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.Test/FileIt.Module.ExampleFlow.Test.csproj
dotnet sln FileIt.All.sln add ./FileIt.Module.ExampleFlow/FileIt.Module.ExampleFlow.Integration/FileIt.Module.ExampleFlow.Integration.csproj
```
