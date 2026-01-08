# cmeraz-fileit

a service bus test

## Quickstart (Github Codespace instructions)

**Note**
This is not yet working in Codespaces.

1. Launch a Codespace from Confirm in VS Code that the image is loaded and the build is complete.
2. Run bash script to submit a test file through the program, scripts/crons_local/simple-test.sh

## Introduction

This repository is a small example project demonstrating a service-bus-focused workflow using .NET and Azure Functions. It contains an API (Azure Function App), unit tests, and an integration test project. The code and scripts are organized to make local development and testing straightforward, including helpers for local storage emulation and sample data generation.

## Top-level projects and folders

- `app/` — Azure Functions project (contains `App.csproj`, module classes organized by folder, configuration files like `host.json` and `local.settings.json`, and deployment/build outputs).
- `databases/` — containing the FileIt.sqlproj SQL project file, for deploying objects and scripts to the mssql database.
- `emulator/` — containing docker files for running the Service Bus emulator.
- `integration/` — A unit test project (`Integration.Test.csproj`) that runs use cases end-to-end.
- `test/` — Unit test project (`Test.csproj`) that mock their dependencies and thus can run in any pipeline or developer machine.
- `scripts/` — Collection of shell scripts used for local setup, azcopy helpers, Azurite startup, cron job examples, and provisioning helpers.

Each folder includes project files and/or scripts to make local dev and CI runs reproducible. If you are exploring the code, start with `app/` to see how functions and business logic interact.

## Notes and tips for Codespaces

- Use the VS Code Tasks panel; the workspace contains a `start` task that runs clean, test, build, and then `func start` for the `api` project. Running the single `start` task will reproduce the behavior above.
- Expose and inspect forwarded ports in the Codespaces Ports view (useful for Functions runtime, local storage emulators, or web UIs).
- Local configuration: prefer `appsettings.Local.json` and `local.settings.json` for developer overrides. Do not commit secrets — use Codespaces secrets or environment variables.
- Storage and blobs: for local storage emulation, use `scripts/azurite/start.sh` to run Azurite, and use `scripts/crons_local/simple-source.sh` to create the sample containers and example blobs used by the project.

# Local Setup
## Requirements
- Visual Studio or VS Code
- MSSQL (Developer edition is fine)
- Azurite (Blob Storage Emulator)
- Docker Desktop
- Service Bus Emulator
- Azure Function Core Tools
- .NET 8

## Installation and Execution
- Install MSSQL.
  - Create a FileIt database using the `scripts/misc/fileit.sql` SQL script.
  - Deploy tables using the dacpac produced by the SQL project
- Install Azurite using npm or the VS Code extension
- Install Docker Desktop
  - Edit the `emulator/config.json` with new queues or topics
  - Run the bash script `emulator/up.sh` to start up the emulator
  - Stop the emulator with `emulator/down.sh`
- Build the solution with `dotnet build`
- Run the solution
  - cd to app/
  - Run `func start`
  - The app/simple/SimpleTest.cs file contains a TimerTrigger that will deposit files in the source container that will trigger the Simple flow

#  Miscelaneous Ideas
BlobClient properties and Service Bus Messages both use Dictionary<string, string> which can be a common means for sharing blob identity, flow, state, and intended address

CorrelationId (correlation-id)	Enables an application to specify a context for the message for the purposes of correlation; for example, reflecting the MessageId of a message that is being replied to.

MessageId (message-id)	The message identifier is an application-defined value that uniquely identifies the message and its payload. The identifier is a free-form string and can reflect a GUID or an identifier derived from the application context. If enabled, the duplicate detection feature identifies and removes second and further submissions of messages with the same MessageId.

ReplyTo (reply-to)	This optional and application-defined value is a standard way to express a reply path to the receiver of the message. When a sender expects a reply, it sets the value to the absolute or relative path of the queue or topic it expects the reply to be sent to.

To (to)	This property is reserved for future use in routing scenarios and currently ignored by the broker itself. Applications can use this value in rule-driven autoforward chaining scenarios to indicate the intended logical destination of the message.

SequenceNumber	The sequence number is a unique 64-bit integer assigned to a message as it is accepted and stored by the broker and functions as its true identifier. For partitioned entities, the topmost 16 bits reflect the partition identifier. Sequence numbers monotonically increase and are gapless. They roll over to 0 when the 48-64 bit range is exhausted. This property is read-only.


Install a local nuget
sudo sh -c 'echo "deb http://archive.ubuntu.com/ubuntu jammy main universe" > /etc/apt/sources.list.d/jammy.list'
sudo apt-get update
sudo apt-get install mono-complete
sudo apt-get install nuget

Configuration
host.json is for function settings
appsettings.json is for non-sensitive application settings
Application Settings in Azure Portal overrides appsettings.json and handles sensitive values
local.settings.json works like the Portal overrides in a non-Azure/local environment

The Service Bus Namespace 

Commands


mkdir FileIt.Common
cd FileIt.Common
dotnet new classlib --name FileIt.Common --framework net8.0
dotnet new mstest --name FileIt.Common.Test --framework net8.0
dotnet new mstest --name FileIt.Common.Integration --framework net8.0
dotnet new sln --name FileIt.Common
dotnet sln FileIt.Common.sln add ./FileIt.Common/FileIt.Common.csproj
dotnet sln FileIt.Common.sln add ./FileIt.Common.Test/FileIt.Common.Test.csproj
dotnet sln FileIt.Common.sln add ./FileIt.Common.Integration/FileIt.Common.Integration.csproj

cd FileIt.Common
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Azure.Storage.Blobs --framework net8.0
dotnet add package Microsoft.Azure.Functions.Worker
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0 --framework net8.0
dotnet add package Microsoft.EntityFrameworkCore.Relational --version 8.0.0 --framework net8.0
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Azure
dotnet add package Serilog
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

cd ./FileIt.Common/bin/Debug
files=( FileIt.Common.*.nupkg )
nuget_file="${files[${#files[@]}-1]}"
nuget push $nuget_file -Source ~/nuget_local



cd ../
dotnet new sln --name FileIt.All
dotnet sln FileIt.All.sln add ./FileIt.Common/FileIt.Common/FileIt.Common.csproj
dotnet sln FileIt.All.sln add ./FileIt.Common/FileIt.Common.Test/FileIt.Common.Test.csproj
dotnet sln FileIt.All.sln add ./FileIt.Common/FileIt.Common.Integration/FileIt.Common.Integration.csproj


mkdir FileIt.App
cd FileIt.App
func init FileIt.App --worker-runtime dotnet-isolated --target-framework net8.0
dotnet new mstest --name FileIt.App.Test --framework net8.0
dotnet new sln --name FileIt.App
dotnet sln FileIt.App.sln add ./FileIt.App/FileIt_App.csproj
dotnet sln FileIt.App.sln add ./FileIt.App.Test/FileIt.App.Test.csproj
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer
dotnet add package Microsoft.Azure.Functions.Worker.Sdk
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


cd FileIt.App
dotnet nuget add source ~/nuget_local -n LocalFeed
dotnet add package FileIt.Common


cd ~/repos/cmeraz-fileit/
dotnet sln FileIt.All.sln add ./FileIt.App/FileIt.App/FileIt_App.csproj
dotnet sln FileIt.All.sln add ./FileIt.App/FileIt.App.Test/FileIt.App.Test.csproj

mkdir FileIt.CsvProvider
cd FileIt.CsvProvider
func init FileIt.CsvProvider --worker-runtime dotnet-isolated --target-framework net8.0
dotnet new mstest --name FileIt.CsvProvider.Test --framework net8.0
dotnet new sln --name FileIt.CsvProvider
dotnet sln FileIt.CsvProvider.sln add ./FileIt.CsvProvider/FileIt_CsvProvider.csproj
dotnet sln FileIt.CsvProvider.sln add ./FileIt.CsvProvider.Test/FileIt.CsvProvider.Test.csproj

cd ../
dotnet sln FileIt.All.sln add ./FileIt.CsvProvider/FileIt.CsvProvider/FileIt_CsvProvider.csproj
dotnet sln FileIt.All.sln add ./FileIt.CsvProvider/FileIt.CsvProvider.Test/FileIt.CsvProvider.Test.csproj


mkdir FileIt.SimpleProvider
cd FileIt.SimpleProvider
func init FileIt.SimpleProvider --worker-runtime dotnet-isolated --target-framework net8.0
dotnet new mstest --name FileIt.SimpleProvider.Test --framework net8.0
dotnet new sln --name FileIt.SimpleProvider
dotnet sln FileIt.SimpleProvider.sln add ./FileIt.SimpleProvider/FileIt_SimpleProvider.csproj
dotnet sln FileIt.SimpleProvider.sln add ./FileIt.SimpleProvider.Test/FileIt.SimpleProvider.Test.csproj

cd ./FileIt.SimpleProvider
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer
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


dotnet remove package FileIt.Common
dotnet add package FileIt.Common


cd ../../
dotnet sln FileIt.All.sln add ./FileIt.SimpleProvider/FileIt.SimpleProvider/FileIt_SimpleProvider.csproj
dotnet sln FileIt.All.sln add ./FileIt.SimpleProvider/FileIt.SimpleProvider.Test/FileIt.SimpleProvider.Test.csproj




rm -r FileIt.All
rm -r FileIt.App
rm -r FileIt.CsvProvider
rm -r FileIt.SimpleProvider

rm FileIt.All.sln
rm FileIt.App.sln
rm FileIt.CsvProvider.sln
rm FileIt.SimpleProvider.sln