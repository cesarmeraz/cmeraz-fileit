# Module generation

FileIt modules follow a fixed shape: Host + App + Test + Integration projects, EventGrid blob watcher, Service Bus subscriber, dead-letter reader, structured logging through the shared infrastructure. New modules differ only in business logic. This system scaffolds everything else.

Closes issue #24.

## One-time setup

Install the template from the repo root:

```powershell
dotnet new install .\templates\FileIt.Module
```

Verify:

```powershell
dotnet new list fileit-module
```

You should see `FileIt Module` listed with short name `fileit-module`.

To uninstall (rare, mostly for template development):

```powershell
dotnet new uninstall .\templates\FileIt.Module
```

## Creating a module

Always use the wrapper script, not `dotnet new` directly. The wrapper does the cross-cutting work the bare template can't: name validation, EventId/port allocation, solution registration, smoke build.

```powershell
.\scripts\new-fileit-module.ps1 -Name TradeRecon
```

That single command:

1. Validates the name (PascalCase, not reserved)
2. Auto-allocates a free EventId block (4000+) and HTTP port (7063+)
3. Calls `dotnet new fileit-module` with all the right substitutions
4. Adds the four projects (`Host`, `App`, `Test`, `Integration`) to `FileIt.All.sln`
5. Runs `dotnet build` against the new module to verify it compiles
6. Prints the three files you should edit first

To preview without writing files:

```powershell
.\scripts\new-fileit-module.ps1 -Name TradeRecon -DryRun
```

To create a slimmer module (e.g. API receiver only, no blob watcher):

```powershell
.\scripts\new-fileit-module.ps1 -Name ApiReceiver -NoWatcher -NoTestSeeder
```

## Removing a module

```powershell
.\scripts\new-fileit-module.ps1 -Name TradeRecon -Remove
```

This removes the four projects from `FileIt.All.sln` and deletes `FileIt.Module.TradeRecon\`. The script refuses to remove `DataFlow`, `Services`, or `SimpleFlow` regardless of arguments.

## What the template generates

| Path | Purpose |
|---|---|
| `FileIt.Module.{Name}.Host/Program.cs` | Worker entry, middleware chain, DI wiring, logging config |
| `FileIt.Module.{Name}.Host/host.json` | Functions runtime + App Insights sampling |
| `FileIt.Module.{Name}.Host/local.settings.json` | Emulator connection strings, schedules |
| `FileIt.Module.{Name}.Host/appsettings.{env}.json` | Per-env feature config |
| `FileIt.Module.{Name}.Host/Health.cs` | HTTP health endpoint |
| `FileIt.Module.{Name}.Host/{Name}Watcher.cs` | EventGrid blob trigger (and `#if DEBUG` BlobTrigger for local) |
| `FileIt.Module.{Name}.Host/{Name}Subscriber.cs` | Service Bus topic subscriber |
| `FileIt.Module.{Name}.Host/{Name}DeadLetterReader.cs` | Drains `api-add-{prefix}-sub/$DeadLetterQueue` into `DeadLetterRecord` |
| `FileIt.Module.{Name}.Host/{Name}Test.cs` | Timer-triggered test seeder for local dev |
| `FileIt.Module.{Name}.App/{Name}Config.cs` | Strongly-typed feature config |
| `FileIt.Module.{Name}.App/{Name}Events.cs` | EventId catalog (1000-block allocated per module) |
| `FileIt.Module.{Name}.App/{Name}Message.cs` | Message DTO |
| `FileIt.Module.{Name}.App/WatchInbound/WatchInbound.cs` | Use-case for watcher |
| `FileIt.Module.{Name}.App/WaitOnApiUpload/BasicApiAddHandler.cs` | Use-case for subscriber |
| `FileIt.Module.{Name}.Test/` | MSTest unit tests |
| `FileIt.Module.{Name}.Integration/` | MSTest integration tests |
| `README.md` | Per-module docs with first-three-files-to-edit |

## Conventions enforced by the template

- `dotnet-isolated` worker, .NET 10, Functions runtime v4
- `MiddlewareLogger` + `SerilogInvocationIdMiddleware` + `ExceptionHandlingMiddleware` middleware chain
- `ICommonLogConfig` from configuration applied to all sinks (App Insights, file, console)
- Service Bus topic `api-add-topic` with subscription `api-add-{prefix}-sub`
- Dead-letter pipeline auto-wired through `IDeadLetterIngestionService`
- EventId blocks allocated per module: `Services=1000, SimpleFlow=2000, DataFlow=3000`, new modules at 4000+

## EventId block reservations

| Block | Module |
|---|---|
| 1000-1999 | Services |
| 2000-2999 | SimpleFlow |
| 3000-3999 | DataFlow |
| 4000-4999 | (next free, auto-allocated) |

The wrapper script's `$ReservedEventIdBlocks` hashtable is the source of truth. Update it when you add a module.

## HTTP port reservations

| Port | Module |
|---|---|
| 7060 | SimpleFlow |
| 7061 | DataFlow |
| 7062 | Services |
| 7063+ | (auto-allocated) |

## Why a `dotnet new` template instead of "copy folder and sed"

- Microsoft's template engine handles conditional content (`IncludeWatcher`, `IncludeSubscriber`, etc.) without leaving stub files behind
- Discoverable via `dotnet new list` so contributors don't need to find a script
- Plays nice with VS / Rider / VS Code project creation dialogs
- Integrates with `dotnet new install` / `uninstall` lifecycle
- Substitution is whitespace-aware, won't accidentally rewrite a substring inside a longer identifier
- The wrapper script handles everything the bare template can't (sln registration, smoke build, port/event allocation), so we get the best of both
