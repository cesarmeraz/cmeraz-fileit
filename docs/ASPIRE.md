# Local development with Aspire

This document describes the optional .NET Aspire AppHost for running the FileIt stack locally. Cesar's original approach (manual docker compose for the emulator, plus three terminals for the function hosts) still works. This adds a one-command alternative that also gives you a unified dashboard with live structured logs across all three hosts.

This is on branch `feature/gl-account-dataflow` in the `Pr0x1mo` fork. It is not yet merged to `develop`.

## What the AppHost gives you

A single `dotnet run` on `FileIt.AppHost` starts:

- Azurite (blob, queue, and table on ports 10000, 10001, 10002)
- The `services-host`, `simpleflow-host`, and `dataflow-host` function apps in the right order (with `WaitFor` dependencies so they do not race Azurite)
- The six blob containers required by DataFlow and SimpleFlow (`dataflow-source`, `dataflow-working`, `dataflow-final`, `simple-source`, `simple-working`, `simple-final`) auto-created on startup
- The Azure SQL `FileItDbConnection` injected into every function host from user secrets (no `local.settings.json` dependency for the connection string)
- An Aspire dashboard at `https://localhost:17199` with Console, Structured logs, Parameters, Traces, and Metrics tabs

Service Bus is not yet wired into the AppHost. Locally, the existing docker compose emulator in `/emulator` still covers service bus until the team gets RBAC grants from Finn for the real Azure Service Bus.

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for Azurite, which Aspire runs as a managed container)
- Visual Studio 2026 (or `dotnet` CLI alone works)
- Azure CLI (`az`) for dropping test blobs
- Access to the Azure SQL database `jmplabsv04.database.windows.net / FileIt` (read/write on `DataFlowRequestLog` at minimum)

## First-time setup

### 1. Trust the Aspire dev cert

Aspire's OTLP endpoint uses `https://localhost:<port>` with a self-signed dev cert. Without this step the cert validation fails silently and structured logs never reach the dashboard:

```powershell
dotnet dev-certs https --trust
```

Confirm on the Windows prompt.

### 2. Set the Azure SQL connection string as a user secret

The AppHost reads the connection string from user secrets, NOT from `appsettings.json` or `local.settings.json`. This keeps the password out of source control:

```powershell
cd FileIt.AppHost
dotnet user-secrets set "ConnectionStrings:azureSql" "Data Source=jmplabsv04.database.windows.net;Initial Catalog=FileIt;User ID=<your_sql_user>;Password=<your_sql_password>;TrustServerCertificate=True;Encrypt=True;"
```

Note: PowerShell will try to interpret `$` as variable markers in your password. If your password contains `$`, escape each one with a backtick.

### 3. Run

```powershell
cd FileIt.AppHost
dotnet run
```

Within a few seconds you should see:

info: Aspire.Hosting.DistributedApplication[0]
Distributed application started. Press Ctrl+C to shut down.
[container-init] ensured: dataflow-source
[container-init] ensured: dataflow-working
[container-init] ensured: dataflow-final
[container-init] ensured: simple-source
[container-init] ensured: simple-working
[container-init] ensured: simple-final

Open the dashboard URL from the earlier log line (it includes an auth token).

## Testing the flow end to end

From a second PowerShell window (leave Aspire running in the first):

```powershell
$conn = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;"
az storage blob upload --container-name dataflow-source --file "<repo_root>\GLAccount.csv" --name GLAccount.csv --connection-string $conn --overwrite
```

Within 10 seconds you should see in the Aspire dashboard:

- **Resources tab**: all 5 resources (`storage`, `blobs`, `dataflow-host`, `services-host`, `simpleflow-host`) Running
- **Console tab on `dataflow-host`**: `DataFlowWatcherLocal` fires, blob moves to `dataflow-working`, message enqueued to the `dataflow-transform` queue, `DataFlowSubscriber` picks it up, `TransformGlAccounts` runs (24 groups), `summary_GLAccount.csv` written to `dataflow-final`
- **Structured tab**: all the above as searchable JSON log entries
- **SSMS against FileIt**: new row in `dataflow.DataFlowRequestLog` with `Status=Complete`, `RowsTransformed=24`

Copy the `ClientRequestId` from the new SQL row and paste it into the Structured tab's filter to see the complete journey for that one CSV across all hosts in time order.

## Gotchas I ran into

### Aspire uses a fresh Azurite every run

Every `dotnet run` on the AppHost spins up a new Azurite container. Unless you configure `.WithDataVolume()` or `.WithDataBindMount(...)`, no blobs or containers from a previous session survive.

That is why the AppHost auto-creates the six containers on startup via an `AfterResourcesCreatedEvent` subscriber, so a fresh clone (or a restart) never requires a manual `az storage container create` step before the pipeline will run.

### The Structured tab is empty until you wire OTLP

Aspire's dashboard reads logs from the OpenTelemetry Logs pipeline, not from `stdout`. The function hosts use Serilog directly with `ClearProviders()`, so there is no built-in OTel log producer. To get logs into the Structured tab I added `Serilog.Sinks.OpenTelemetry` to the Infrastructure project and extended `CommonLogExtensions.AddCommonLog` to write to OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set in the environment (Aspire injects this automatically into each child process).

When run standalone (without Aspire), the env var is absent so the sink is not registered and behavior is identical to the original code. Nothing for non-Aspire developers breaks.

### Dev cert TLS handshake failures

Aspire's OTLP endpoint is `https://localhost:<port>`. If you do not run `dotnet dev-certs https --trust`, the handshake fails silently. The Serilog sink swallows the error unless Serilog SelfLog is enabled.

For belt-and-suspenders, the sink configuration also installs an `HttpClientHandler` with `DangerousAcceptAnyServerCertificateValidator` to handle the case where someone skips the trust step. This is safe for localhost dev but should never ship to production. The `#if RELEASE` block around the Application Insights sink remains untouched for the production path.

### Serilog File sink contention

All three function hosts originally had `LOG_FILE_PATH` pointing to the same `log.txt` in the repo root. Serilog's File sink is single-writer. When all three hosts started concurrently under Aspire they fought for the file lock and every log event triggered a `Serilog.Sinks.File.FailedSink: the sink could not be initialized` error. Also the Serilog SelfLog (which we enabled to diagnose the OTLP issue) amplified this noise.

Fix: removed `LOG_FILE_PATH` and `SERILOG_SELFLOG_FILE_PATH` from all three `local.settings.json` files. Console sink + Aspire OTLP + the existing SQL `DatabaseSink` cover every logging need for local dev.

### AddBlobContainer does not exist in Aspire 13.2.2

Early in the build I tried `blobs.AddBlobContainer("dataflow-source")` based on a guess at the API. It did not compile. Container auto-creation is done instead inside an `AfterResourcesCreatedEvent` subscription using `Azure.Storage.Blobs`, with a retry loop to cover the brief window between "Aspire says the storage resource is created" and "Azurite is actually listening on port 10000".

### IDistributedApplicationLifecycleHook is deprecated in Aspire 13

Early drafts used `IDistributedApplicationLifecycleHook.AfterResourcesCreatedAsync`. In Aspire 13 this interface is marked `[Obsolete]` in favor of the eventing API. The AppHost now uses `builder.Eventing.Subscribe<AfterResourcesCreatedEvent>(...)` instead, which is the modern equivalent.

### Local SQL was briefly included by mistake

An early experimental AppHost had `builder.AddSqlServer("sql").WithDataVolume()` to spin up a local containerized SQL Server. That was a wrong turn. FileIt's logging and request-log tables are in Azure SQL in the cloud (`jmplabsv04`), not local. The AppHost now uses `builder.AddConnectionString("azureSql")` to pass through the cloud connection string instead.

## File map

| File | Purpose |
|---|---|
| `FileIt.AppHost/AppHost.cs` | Aspire orchestration: Azurite, three function hosts, Azure SQL connection reference, blob container auto-init |
| `FileIt.AppHost/FileIt.AppHost.csproj` | Aspire SDK + `Azure.Storage.Blobs` + (unused for now) `Aspire.Hosting.Azure.ServiceBus` + `Aspire.Hosting.SqlServer` |
| `FileIt.Infrastructure/FileIt.Infrastructure/Extensions/CommonLogExtensions.cs` | `AddCommonLog` extended with the OTLP sink block that fires only when `OTEL_EXPORTER_OTLP_ENDPOINT` is present |
| `FileIt.Module.*.Host/local.settings.json` | `LOG_FILE_PATH` and `SERILOG_SELFLOG_FILE_PATH` removed |

## Known open items on this work

- **Traces tab is empty.** Structured logs work. Distributed traces would require adding OpenTelemetry `ActivitySource` instrumentation to each function invocation and registering an `OtlpExporter` in each host's DI container. Medium effort, high demo value (you would see a waterfall of `WatchInbound -> queue hop -> DataFlowSubscriber -> TransformGlAccounts` in one view).

- **Service Bus in AppHost.** `Aspire.Hosting.Azure.ServiceBus` is already referenced in the `csproj`. Once Finn grants the Service Bus RBAC role, we can add `builder.AddAzureServiceBus("servicebus").RunAsEmulator()` + `AddServiceBusQueue(...)` + `AddServiceBusTopic(...)` calls and `WithReference(serviceBus)` on each function host. Until then, the existing docker compose emulator in `/emulator` still works alongside Aspire.

- **Lifecycle hook refactor.** Done. Container init is inside an eventing subscriber, not a `Task.Run` with a fixed delay.


## Cloud Service Bus provisioning (2026-04-22)

The cloud Service Bus was provisioned today in the Innovation Hub hackathon tenant:

- Tenant: `innohubspace.onmicrosoft.com`
- Subscription: `lab-35`
- Resource group: `rg-lab-sbus-01`
- Namespace: `sbus-pe-2d99722c9843d8` (Premium, Canada Central, zone-redundant)
- Host: `sbus-pe-2d99722c9843d8.servicebus.windows.net`

### Entities created via Azure CLI

```powershell
$ns = "sbus-pe-2d99722c9843d8"
$rg = "rg-lab-sbus-01"

az servicebus queue create --namespace-name $ns --resource-group $rg --name dataflow-transform
az servicebus queue create --namespace-name $ns --resource-group $rg --name api-add

az servicebus topic create --namespace-name $ns --resource-group $rg --name dataflow-transform-topic
az servicebus topic create --namespace-name $ns --resource-group $rg --name api-add-topic

az servicebus topic subscription create --namespace-name $ns --resource-group $rg --topic-name api-add-topic --name api-add-simple-sub
```

Final entity list:

| Kind | Name | Notes |
|---|---|---|
| Queue | `dataflow-transform` | DataFlow WatchInbound → Subscriber |
| Queue | `api-add` | SimpleFlow → ApiAdd |
| Queue | `secure-queue` | Pre-existing (Finn's test artifact, unused by app) |
| Topic | `dataflow-transform-topic` | DataFlow reply channel |
| Topic | `api-add-topic` | Services ApiAdd response topic |
| Topic Subscription | `api-add-simple-sub` on `api-add-topic` | SimpleSubscriber listener |

### Gotcha - public network access disabled by default

The namespace name prefix `sbus-pe-` is a convention hinting at "private endpoint". Out of the box the namespace had **Public network access = Disabled**, which blocks Peek/Send/Receive from anything not inside the allowed VNet. Service Bus Explorer in the portal showed a yellow banner: "This namespace has public network access disabled, data operations such as Peek, Send or Receive against this Service Bus entity will not work until you switch to all networks or allowlist your client IP."

For local dev and demo we switched to **Public network access = Enabled from all networks** via the Networking blade. Acceptable for this sandbox (fake users, terminated after hackathon). For production you would leave public access off and connect only from inside the VNet via managed identity on deployed function apps.

### Auth for local dev

SAS via `RootManageSharedAccessKey` policy (claims: Manage, Send, Listen). Primary Connection String copied into user secrets rather than source control:

```powershell
cd FileIt.AppHost
dotnet user-secrets set "ConnectionStrings:serviceBus" "<primary connection string>"
```

For cloud deployment the function apps should use their managed identity (`mi-fileit-*`) assigned the `Azure Service Bus Data Owner` role, not SAS.
