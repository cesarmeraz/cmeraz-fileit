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


## Create the Projects
```bash
mkdir FileIt.ExampleFlow
cd ~/repos/cmeraz-fileit/FileIt.ExampleFlow
func init FileIt.ExampleFlow --worker-runtime dotnet-isolated --target-framework net8.0
dotnet new classlib --name FileIt.ExampleFlow.App --framework net8.0
dotnet new mstest --name FileIt.ExampleFlow.Test --framework net8.0
dotnet new mstest --name FileIt.ExampleFlow.Integration --framework net8.0
```

## Add Packages to the Function App
```bash
cd ~/repos/cmeraz-fileit/FileIt.ExampleFlow/FileIt.ExampleFlow
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventGrid
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer
dotnet add package Microsoft.Azure.WebJobs
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Hosting.Abstractions
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
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
cd ~/repos/cmeraz-fileit/FileIt.ExampleFlow/FileIt.ExampleFlow
dotnet add reference ../../FileIt.Infrastructure/FileIt.Infrastructure/FileIt.Infrastructure.csproj 
dotnet add reference ../../FileIt.ExampleFlow/FileIt.ExampleFlow.App/FileIt.ExampleFlow.App.csproj 
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
```

## Add Project References to the Function App Handler
```bash
cd ~/repos/cmeraz-fileit/FileIt.ExampleFlow/FileIt.ExampleFlow.App
dotnet add reference ../../FileIt.Domain/FileIt.Domain/FileIt.Domain.csproj 
```

## Add Project References and Packages to the Test project
```bash
cd ~/repos/cmeraz-fileit/FileIt.ExampleFlow/FileIt.ExampleFlow.Test
dotnet add reference ../../FileIt.Infrastructure/FileIt.Infrastructure/FileIt.Infrastructure.csproj 
dotnet add reference ../../FileIt.Domain/FileIt.Domain/FileIt.Domain.csproj 
dotnet add reference ../FileIt.ExampleFlow.App/FileIt.ExampleFlow.App.csproj 
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package Moq
```

## (Re)Create the Module Solution file
```bash
cd ~/repos/cmeraz-fileit/FileIt.ExampleFlow
rm FileIt.ExampleFlow.sln
dotnet new sln --name FileIt.ExampleFlow
dotnet sln FileIt.ExampleFlow.sln add ./FileIt.ExampleFlow/FileIt_ExampleFlow.csproj
dotnet sln FileIt.ExampleFlow.sln add ./FileIt.ExampleFlow.App/FileIt.ExampleFlow.App.csproj
dotnet sln FileIt.ExampleFlow.sln add ./FileIt.ExampleFlow.Test/FileIt.ExampleFlow.Test.csproj
dotnet sln FileIt.ExampleFlow.sln add ./FileIt.ExampleFlow.Integration/FileIt.ExampleFlow.Integration.csproj
```

## Add the Module to the main Solution
```bash
cd ~/repos/cmeraz-fileit/
dotnet sln FileIt.All.sln add ./FileIt.ExampleFlow/FileIt.ExampleFlow/FileIt_ExampleFlow.csproj
dotnet sln FileIt.All.sln add ./FileIt.ExampleFlow/FileIt.ExampleFlow.App/FileIt.ExampleFlow.App.csproj
dotnet sln FileIt.All.sln add ./FileIt.ExampleFlow/FileIt.ExampleFlow.Test/FileIt.ExampleFlow.Test.csproj
dotnet sln FileIt.All.sln add ./FileIt.ExampleFlow/FileIt.ExampleFlow.Integration/FileIt.ExampleFlow.Integration.csproj
```
