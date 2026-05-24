---
marp: true
theme: gaia
paginate: true
size: 16:9
title: cmeraz-fileit
description: Windows service migration to Azure-native serverless workflows
---

# cmeraz-fileit
## Migrating a Windows service to Azure Service Bus + Functions

- Proof of concept for cloud-native workflow processing
- Modular function apps, shared platform resources
- Built to run in Azure and locally with emulators

---

# Why this exists

Legacy pain points (real project constraints):

- Manual deployment and fragile operations
- Limited observability and no fast RCA loop
- Heavy-load failures without safe load leveling
- Hard-to-test architecture with scattered repos

Target outcome:

- Repeatable delivery, scalable execution, and traceable workflows

---

# What makes this different

- Multi-module serverless architecture with clear boundaries
- Shared infrastructure, independent workflow deployment
- Local-first dev with Azurite + Service Bus emulator + SQL
- Strong message and event conventions for traceability
- Dead-letter lifecycle designed as an operational feature, not an afterthought

---

# High-level architecture

Core components:

- Function Apps (Services, SimpleFlow, DataFlow)
- Service Bus (queues/topics for decoupling + load leveling)
- Blob Storage (source/working/final containers)
- Azure SQL (request logs + dead-letter records)
- Application Insights (cross-host observability)
- User-defined managed identities (platform auth)

---

# Flow in 9 steps (SimpleFlow)

1. Test trigger drops file in source container
2. Watcher receives blob event
3. Request log created with CorrelationId
4. File moved to working
5. Message sent to `api-add` queue
6. Services module simulates API and publishes result
7. Subscriber reads topic subscription
8. Request log updated by CorrelationId
9. File moved to final container

---

# Code example: watcher trigger + correlation scope

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
