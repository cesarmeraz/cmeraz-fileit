
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
  - Run the bash script `scripts/azurite/create-containers.sh` for the simple flow containers.
- Install Docker Desktop
  - Edit the `emulator/config.json` with new queues or topics
  - Run the bash script `emulator/up.sh` to start up the emulator
  - Stop the emulator with `emulator/down.sh`
- Build the solution with `dotnet build`
- Run the solution
  - cd to FileIt.Module.SimpleFlow/FileIt.Module.SimpleFlow.Host
  - Run `func start`
  - The SimpleTest.cs file contains a TimerTrigger that will deposit files in the source container that will trigger the Simple flow