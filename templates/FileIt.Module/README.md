# FileIt.Module.FileItModule

Scaffolded by `dotnet new fileit-module`.

## Structure

| Project | Role |
|---|---|
| `FileIt.Module.FileItModule.Host` | Azure Functions host. Contains the function entry points (Health, Watcher, Subscriber, DeadLetterReader, Test). |
| `FileIt.Module.FileItModule.App` | Application logic. Contains the use-case classes (WatchInbound, BasicApiAddHandler), config, events, message DTOs. |
| `FileIt.Module.FileItModule.Test` | Unit tests for the App project (MSTest). |
| `FileIt.Module.FileItModule.Integration` | Integration tests (MSTest, run against the emulator stack). |

## First three files to edit

1. **`FileIt.Module.FileItModule.App/FileItModuleEvents.cs`** — confirm your EventId block doesn't collide with another module's. Existing blocks: SimpleFlow=2000, DataFlow=3000, Services=1000.
2. **`FileIt.Module.FileItModule.App/WatchInbound/WatchInbound.cs`** — replace the boilerplate "move blob, log it, queue an API call" with whatever your module actually does on inbound.
3. **`FileIt.Module.FileItModule.Host/appsettings.json`** — adjust `QueueName`, container names, and EventId values to match your business semantics.

## Local dev

```bash
dotnet build
cd FileIt.Module.FileItModule.Host
func start
```

The local HTTP port is set in `Properties/launchSettings.json`. Adjust if it collides with another running module.

## Conventions enforced by this template

- `dotnet-isolated` worker, .NET 10, Functions runtime v4
- Service Bus topic + subscription pattern: `api-add-topic` / `api-add-fileitmodule-sub`
- Dead-letter pipeline auto-wired via `IDeadLetterIngestionService` (see `FileItModuleDeadLetterReader.cs`)
- Logging via `MiddlewareLogger` + `SerilogInvocationIdMiddleware` + `ExceptionHandlingMiddleware`
- `ICommonLogConfig` populated from configuration, applied to all sinks (App Insights, file, console)

## Wiring into the solution

The wrapper script `scripts/new-fileit-module.ps1` adds these projects to `FileIt.All.sln` automatically. If you scaffold from `dotnet new` directly, run:

```powershell
dotnet sln FileIt.All.sln add FileIt.Module.FileItModule/FileIt.Module.FileItModule.Host/FileIt.Module.FileItModule.Host.csproj
dotnet sln FileIt.All.sln add FileIt.Module.FileItModule/FileIt.Module.FileItModule.App/FileIt.Module.FileItModule.App.csproj
dotnet sln FileIt.All.sln add FileIt.Module.FileItModule/FileIt.Module.FileItModule.Test/FileIt.Module.FileItModule.Test.csproj
dotnet sln FileIt.All.sln add FileIt.Module.FileItModule/FileIt.Module.FileItModule.Integration/FileIt.Module.FileItModule.Integration.csproj
```
