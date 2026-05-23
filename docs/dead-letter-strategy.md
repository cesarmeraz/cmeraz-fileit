# FileIt Dead Letter Strategy

This document defines how FileIt detects, persists, investigates, and resolves messages that fail processing in Azure Service Bus.

This is the implementation plan for [#22 - Dead Letter Strategy](https://github.com/Pr0x1mo/cmeraz-fileit/issues/22).

## What is a dead letter?

Azure Service Bus queues and subscriptions have a built-in dead-letter sub-queue (DLQ). A message lands there when:

1. **MaxDeliveryCount is exceeded.** Default is 5 in Service Bus. Each time a function fails to process the message and the lock expires (or the function explicitly abandons), the delivery count increments. After the 5th failed attempt, Service Bus moves the message to `<queue>/$DeadLetterQueue`.
2. **TTL expires.** Messages older than `TimeToLive` get dead-lettered with reason `TTLExpiredException`.
3. **Explicit dead-letter.** Code can call `DeadLetterMessageAsync` to immediately route a message to DLQ with a custom reason and description (e.g., "schema validation failed: missing required field X").

In all three cases, the original queue stops attempting to process the message, and it sits in the DLQ until something explicitly handles it. **By default, FileIt does nothing with DLQ messages — they accumulate forever, invisible to ops, devs, and QA.** This document defines what we do instead.

## Principles

1. **Observability first.** A dead letter is a signal, not a failure to hide. Every DLQ message must be visible in CommonLog, in a dedicated `DeadLetterRecord` table, and surfaced in the FileIt UI (#17). No DLQ message goes unnoticed.

2. **Categorize before acting.** Not every DLQ message deserves the same response. We classify each into one of:
   - **Transient** — cause was infra (timeout, throttling, dependency down). Retry is appropriate.
   - **Permanent** — cause was data (malformed payload, missing record, business rule violation). Retry will fail again. Manual intervention or discard.
   - **Poison** — cause is unknown or so dangerous that automatic action is risky (corrupt deserialization, security violation). Surface for human review only.

3. **Retry is opt-in, not automatic.** Automatic retry-of-DLQ is a foot-gun: the same message that just failed 5 times can fail 5 more, and infinite-loop it back to DLQ. We provide a retry mechanism but require an explicit human or scheduled action to invoke it.

4. **Persist the full message.** Service Bus DLQ messages have a finite TTL themselves. We copy the full body and metadata into our SQL `DeadLetterRecord` table so we have an immutable record even after Service Bus expires it.

5. **Resolution is tracked.** Every dead letter has a status lifecycle (Pending → Retried | Discarded | Resolved) and an audit trail (when, why, by whom).

## Architecture
Production queue (e.g. dataflow-transform)
│
│ 5 failed attempts
▼
Dead-letter sub-queue (dataflow-transform/$DeadLetterQueue)
│
│ Triggered by DeadLetterReader function
▼
DeadLetterReader function (one per source queue/topic)
│  ├── Writes DeadLetterRecord row to SQL
│  ├── Logs to CommonLog with new EventIds
│  └── Completes the DLQ message (so DLQ doesn't fill)
▼
DeadLetterRecord table (persistent, queryable)
│
│ Surfaces via:
├── FileIt UI dead-letter inbox (#17)
├── SQL queries (docs/queries/deadletters/)
└── Operational runbook (docs/runbooks/dlq-incident-response.md)
Resolution paths (manual, opt-in):
├── Retry → resubmits original payload to source queue, marks Retried
├── Discard → marks Discarded with reason, no further action
└── Resolved → marks Resolved when underlying issue is fixed (no resubmit needed)
## Components

### 1. DeadLetterRecord table

Persistent SQL record of every dead-lettered message. One row per DLQ event.
Id                       int identity PK
SourceQueue              nvarchar(200)  -- e.g. "dataflow-transform" or "api-add-topic/api-add-simple-sub"
OriginalMessageId        nvarchar(200)
CorrelationId            nvarchar(100)  -- so we can link back to the originating flow in CommonLog
SequenceNumber           bigint
EnqueuedTimeUtc          datetime2
DeadLetteredTimeUtc      datetime2
DeliveryCount            int            -- count when it was dead-lettered (typically MaxDeliveryCount)
DeadLetterReason         nvarchar(500)  -- from BrokerProperties.DeadLetterReason
DeadLetterErrorDescription nvarchar(max)
ContentType              nvarchar(100)
MessageBody              nvarchar(max)  -- full original payload, captured before DLQ TTL expires
ApplicationProperties    nvarchar(max)  -- JSON of the original AppProperties dict
FailureCategory          nvarchar(50)   -- 'Transient', 'Permanent', 'Poison', 'Unknown'
Status                   nvarchar(50)   -- 'Pending', 'Retried', 'Discarded', 'Resolved'
ResolutionNote           nvarchar(max)
LastActionAt             datetime2
LastActionBy             nvarchar(200)
CreatedOn                datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
ModifiedOn               datetime2 NOT NULL DEFAULT SYSUTCDATETIME()

Indexes:
- `IX_DeadLetterRecord_Status_CreatedOn` for the dead-letter inbox view (Pending first, newest first)
- `IX_DeadLetterRecord_CorrelationId` for cross-referencing with CommonLog
- `IX_DeadLetterRecord_SourceQueue_Status` for per-queue dashboards

### 2. DeadLetterReader functions

One Azure Function per DLQ source. Triggers on the `$DeadLetterQueue` of each production queue/subscription.

Source queues to monitor:
- `dataflow-transform/$DeadLetterQueue` (DataFlow host)
- `api-add/$DeadLetterQueue` (Services host)
- `api-add-topic/api-add-simple-sub/$DeadLetterQueue` (SimpleFlow host)

Each reader function:
1. Receives the dead-lettered message via `[ServiceBusTrigger("queue/$DeadLetterQueue")]`.
2. Reads broker-set properties: `DeadLetterReason`, `DeadLetterErrorDescription`, `DeliveryCount`, `EnqueuedTimeUtc`, etc.
3. Extracts `CorrelationId` from `ApplicationProperties` if present, falls back to message-level `CorrelationId`.
4. Categorizes the failure (see categorization heuristics below).
5. Writes a `DeadLetterRecord` row to SQL.
6. Logs the event to CommonLog with the new `DeadLetterReceived` EventId.
7. Completes the DLQ message so the DLQ itself doesn't fill up.

### 3. Failure categorization heuristics

Initial rules (refined over time as we see real failures):

| Reason / Description pattern                                  | Category    |
|---------------------------------------------------------------|-------------|
| `TTLExpiredException`                                          | Transient   |
| `MaxDeliveryCountExceeded` AND error description contains `Timeout`, `Throttle`, `transient`, `retry`, `unavailable` | Transient   |
| `MaxDeliveryCountExceeded` AND description contains `Validation`, `Schema`, `NotFound`, `Forbidden`, `Unauthorized`, `Format` | Permanent   |
| Explicit dead-letter with reason `PoisonMessage` or `Corrupt`  | Poison      |
| Anything else                                                  | Unknown     |

`Unknown` defaults to manual triage. We do NOT auto-retry Unknown to avoid foot-guns.

### 4. Retry mechanism

A retry takes a `DeadLetterRecord` row and resubmits its `MessageBody` and `ApplicationProperties` to the original `SourceQueue`. Implementation: a service method `IDeadLetterRetryService.RetryAsync(int recordId, string actionBy, string note)` that:

1. Loads the record. If status is not `Pending`, refuse (don't double-retry).
2. Constructs a new `ServiceBusMessage` from the captured body and props.
3. Sends it to the source queue using the existing `BusTool` or `PublishTool` based on whether the source is a queue or topic.
4. Updates the record: `Status='Retried'`, `LastActionAt=now`, `LastActionBy=actionBy`, `ResolutionNote=note`.
5. Logs to CommonLog with `DeadLetterRetried` EventId.

Retry is invoked by:
- A UI button per dead-letter row (in #17)
- A SQL stored procedure for ops to call manually
- Future: a scheduled job for `Transient` category records older than N minutes (NOT in this iteration — opt-in only for now)

### 5. Discard mechanism

For `Permanent` and `Poison` records that we've decided not to retry. Same shape as retry but:

1. Loads the record. If status is not `Pending`, refuse.
2. Updates: `Status='Discarded'`, `LastActionBy`, `LastActionAt`, `ResolutionNote` (must include reason).
3. Logs to CommonLog with `DeadLetterDiscarded` EventId.
4. Does NOT resubmit the message.

### 6. Deliberate failure scenario for testing/demo

We need a way to force a message to DLQ on demand to test the pipeline and demo the feature. Approach: a magic marker in the message body that, when seen by a function handler, throws an exception deterministically. After 5 retries, Service Bus dead-letters it.

In `TransformGlAccounts`:
```csharp
if (entry.BlobName?.Contains("FORCE_DLQ_TEST") == true)
{
    throw new InvalidOperationException("Deliberate failure for DLQ testing");
}
```

For demo: drop a CSV named `FORCE_DLQ_TEST.csv` into `dataflow-source`, watch it fail 5 times, watch it appear in the dead-letter inbox.

### 7. New EventIds in InfrastructureEvents.cs

public static EventId DeadLetterReceived       = new EventId(50, nameof(DeadLetterReceived));
public static EventId DeadLetterClassified     = new EventId(51, nameof(DeadLetterClassified));
public static EventId DeadLetterPersisted      = new EventId(52, nameof(DeadLetterPersisted));
public static EventId DeadLetterPersistFailed  = new EventId(53, nameof(DeadLetterPersistFailed));
public static EventId DeadLetterRetryRequested = new EventId(60, nameof(DeadLetterRetryRequested));
public static EventId DeadLetterRetried        = new EventId(61, nameof(DeadLetterRetried));
public static EventId DeadLetterRetryFailed    = new EventId(62, nameof(DeadLetterRetryFailed));
public static EventId DeadLetterDiscarded      = new EventId(63, nameof(DeadLetterDiscarded));
public static EventId DeadLetterResolved       = new EventId(64, nameof(DeadLetterResolved));
public static EventId UnhandledException      = new EventId(70, nameof(UnhandledException)); // also closes #41 issue 3

EventId 70 closes a gap identified in the #41 schema review where `ExceptionHandlingMiddleware` logs unhandled exceptions without an EventId.

### 8. SQL queries (docs/queries/deadletters/)

Six queries mirroring the CommonLog query library structure:

- `01-pending-inbox.sql` — Pending dead letters newest-first, paged
- `02-record-detail.sql` — full record by Id with related CommonLog timeline
- `03-by-source-queue.sql` — counts and latest by source queue
- `04-by-category.sql` — counts by FailureCategory and Status
- `05-stale-pending.sql` — Pending records older than N hours (alerting candidate)
- `06-resolution-history.sql` — recently retried/discarded/resolved with action audit

### 9. Operational runbook

`docs/runbooks/dlq-incident-response.md` — step-by-step what an on-call engineer does when paged about DLQ activity:

1. Acknowledge the alert.
2. Open the FileIt UI dead-letter inbox.
3. Look at FailureCategory and DeadLetterReason.
4. For Transient: review related CommonLog timeline by CorrelationId, decide if root cause is resolved, retry if yes.
5. For Permanent: identify the data issue, fix at source if possible, discard the DLQ record with a note.
6. For Poison/Unknown: do not retry. Open a ticket with engineering. Discard or leave Pending pending engineering review.
7. Document resolution in the record's ResolutionNote.

## Out of scope for this iteration

These belong to follow-up issues:

- **Auto-retry for Transient category** — needs careful backoff and circuit-breaker design. Manual-only for now.
- **DLQ for Azure Function bindings other than Service Bus** — only SB triggers in scope.
- **DLQ alerting via App Insights / PagerDuty** — wire up later when #40 lands.
- **Bulk operations** ("retry all stale Transient older than 1 hour") — single-record only for now.
- **DLQ retention policy** — `DeadLetterRecord` rows accumulate forever in this iteration. Will define retention separately.

## Implementation order

1. This design document (you are here)
2. Add `DeadLetterRecord` entity + DbContext mapping
3. ALTER DATABASE to add `DeadLetterRecord` table + indexes
4. Add new EventIds to `InfrastructureEvents.cs`
5. Create `IDeadLetterPersistenceService` and implementation
6. Create `IDeadLetterClassifier` and implementation with the heuristic table
7. Create three `DeadLetterReader` functions, one per source queue/topic
8. Create `IDeadLetterRetryService` and implementation
9. Add the `FORCE_DLQ_TEST` magic marker to TransformGlAccounts
10. Write 6 SQL queries
11. Write the operational runbook
12. Update `ExceptionHandlingMiddleware` to use the new `UnhandledException` EventId
13. Verify end-to-end: drop FORCE_DLQ_TEST.csv, watch it fail 5x, appear in DeadLetterRecord, retry it, watch it land in original queue.
14. Commit and push as a single rich-message commit.