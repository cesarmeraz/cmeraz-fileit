---
marp: true
theme: default
paginate: true
size: 16:9
title: FileIt
description: Windows service migration to Azure-native serverless workflows

---
# FileIt
## Migrating a Windows service to Azure Service Bus + Functions

- Proof of concept for cloud-native workflows
- Modular function apps, shared platform resources
- Built to run locally with emulators

---
<!-- class: -->
# Why this exists

## Legacy pain points (real project constraints):

- Manual deployment and fragile operations
- Limited observability / limited root cause analysis
- Heavy-load failures without safe load leveling
- Scattered repos: hard to test, refactor, improve

## Target outcome:

- Repeatable delivery, scalable execution, and traceable workflows

---

# What makes this different

- Available for DevSecOps pipelines
- Structured logs with multiple sinks
- Short-lived, asynchronous queue processing
- Dead-letter lifecycle designed as an operational feature, not an afterthought
- Single repository

---

# High-level architecture
- Each module accesses shared resources with its own Managed Identity
- Module communication mediated by messages to Service Bus
<style scoped>
/* Scale up the diagram container and center it */
div.mermaid {
  width: 90%;
  height: 80%;
  margin: 0 auto;
}
div.mermaid svg {
  width: 100% !important;
  height: 100% !important;
  max-width: 100% !important;
}
</style>
<div class="mermaid">
block
  columns 4
    block:common:2
      columns 1
      FA1["FileIt.Module.Services"] 
      MI1<["Managed Identity 1"]>(down) 
      end
    block:simple:2
      columns 1
      FA2["FileIt.Module.Simple"] 
      MA2<["Managed Identity 2"]>(down) 
    end
  block:shared:4
    DB["Azure SQL Database"] 
    SB["Service Bus"] 
    BS["Blob Storage"] 
    AI["App Insights"]
  end
  classDef shape color:black, stroke-width:1px, stroke:black
  class FA1,MI1,FA2,MA2,DB,SB,BS,AI shape
  style common fill:cornflowerblue,stroke-width:4px
  style simple fill:coral,stroke-width:4px,
  style shared fill:goldenrod,stroke-width:4px
</div>

---

# Service Bus Sequence

<style scoped>
.cols {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  align-items: start;
}
.flow {
  font-size: 0.95em;
}
.flow ol {
  margin-top: 0;
}
.diagram {
  padding-top: 4px;
}
.diagram .mermaid {
  width: 100%;
}
.diagram .mermaid svg {
  width: 100% !important;
  height: auto !important;
  max-width: 100% !important;
}
</style>
<div class="cols">
<div class="flow">

- File ingestion triggers the flow
- Mail check pattern using blob URI
- Flow traceable with CorrelationId 
- Service Bus messages require blob URI and CorrelationId

</div>
<div class="diagram">
<div class="mermaid">
%%{init: {'theme': 'forest' } }%%
sequenceDiagram
  autonumber
  participant SF as SimpleFlow
  participant SB as Service Bus
  participant SVC as Services Module

  SF->>SB: Enqueue message

  SB->>SVC: Deliver queued message
  SVC->>SVC: Process request
  SVC->>SVC: Call downstream API
  SVC->>SB: Publish response event

  SB->>SF: Subscribe to response
  SF-->>SF: Complete flow

</div>
</div>
</div>

---

# <!-- fit --> Code example: watcher trigger + correlation scope

```csharp
[Function("DataFlowWatcherLocal")]
public async Task RunLocal(
	[BlobTrigger("dataflow-source/{blobName}")] BlobClient blobClient,
	string blobName,
	FunctionContext context)
{
	string clientRequestId = await blobClient.GetCorrelationId();

	using (_logger.BeginScope(new Dictionary<string, object>
	{
		{ "CorrelationId", clientRequestId }
	}))
	{
		await _watcher.RunAsync(blobName, clientRequestId, context.CancellationToken);
	}
}
```

Source: `FileIt.Module.DataFlow.Host/DataFlowWatcher.cs`

---

# Code example: event IDs as a first-class contract

```csharp
public static EventId DataFlowWatcher = new EventId(3000, nameof(DataFlowWatcher));
public static EventId DataFlowWatcherAddRequestLog = new EventId(3001, nameof(DataFlowWatcherAddRequestLog));
public static EventId DataFlowWatcherMoveToWorking = new EventId(3002, nameof(DataFlowWatcherMoveToWorking));
public static EventId DataFlowWatcherQueueTransform = new EventId(3003, nameof(DataFlowWatcherQueueTransform));
```

Why it matters:

- Predictable event catalog per module
- Easier dashboards, alerts, and timeline reconstruction

Source: `FileIt.Module.DataFlow.App/DataFlowEvents.cs`

---

# Code example: dead-letter reader pattern

```csharp
[Function(nameof(DataFlowDeadLetterReader))]
public async Task Run(
	[ServiceBusTrigger("dataflow-transform/$deadletterqueue")]
	ServiceBusReceivedMessage message,
	FunctionContext context)
{
	var envelope = BuildEnvelope(message);
	await _ingestion.IngestWithoutResultAsync(envelope, context.CancellationToken);
}
```

Design intent:

- Thin function adapter
- Centralized classification + persistence service
- Same pattern reused across modules

Source: `FileIt.Module.DataFlow.Host/DataFlowDeadLetterReader.cs`

---

# Dead-letter strategy (operationally useful)

Message categories:

- Transient
- Permanent
- Poison
- Unknown

Lifecycle:

`Pending -> PendingReplay -> Replayed -> Resolved`

or

`Pending -> Discarded`

Each action is logged and auditable in SQL + CommonLog.

---

# Local dev experience

Local stack options:

- Existing approach: emulator + separate function hosts
- Optional Aspire AppHost: one command + unified dashboard

What this unlocks:

- Faster onboarding
- Reproducible runs
- Better multi-host log visibility

---

# Module generation as a platform feature

```powershell
.\scripts\new-fileit-module.ps1 -Name TradeRecon
```

Generates:

- Host + App + Test + Integration projects
- EventId block + port allocation
- Solution wiring + smoke build

Result: new modules ship with architecture guardrails by default.

---

# Deployment approach (Flex Consumption)

For each module:

- `dotnet publish` creates deployable output
- package zipped via MSBuild target
- OneDeploy uploads zip to function app

Database:

- SQL project / DACPAC-style deployment path

Trade-off:

- Simple and effective for this phase
- Can evolve into enterprise-grade CI/CD later

---

# Placeholder: architecture screenshot

TODO: add screenshot of current architecture diagram

Suggested path:

`./slides/assets/architecture-overview.png`

```markdown
![w:1500](./slides/assets/architecture-overview.png)
```

---

# Placeholder: screencast (local run)

TODO: add short screencast clip/GIF showing:

- `dotnet run` in AppHost (or `func start` flows)
- file dropped in source container
- end-to-end completion in logs

Suggested path:

`./slides/assets/local-run.gif`

---

# Placeholder: log timeline

TODO: add screenshot of correlated logs by `CorrelationId`

Include:

- watcher start
- queue publish
- subscriber receive
- completion / dead-letter event

Suggested path:

`./slides/assets/correlation-log-timeline.png`

---

# Placeholder: DLQ investigation

TODO: add screenshot of one `DeadLetterRecord` + corresponding timeline query

Show:

- category
- status transition
- operator action note

Suggested path:

`./slides/assets/dlq-record-detail.png`

---

# Current status and next steps

Now:

- Core architecture and patterns are in place
- Slide deck scaffold is ready for pipeline testing

Next:

1. Wire assets (screenshots/screencasts/logs)
2. Trim/expand per audience and timebox
3. Publish via GitHub Pages workflow and iterate

---

# Q&A

If useful, next revision can include:

- one slide per module
- failure demo script (FORCE_DLQ_TEST)
- architecture comparison vs. lift-and-shift VM approach
