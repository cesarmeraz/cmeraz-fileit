# Dead Letter Strategy

**Status:** Design
**Owner:** Platform Engineering (FileIt)
**Last updated:** April 24, 2026
**Related issues:** #22 (this doc), #28/#39 (cloud Service Bus), #41 (CommonLog), #53 (EventId Name)

## 1. Why this exists

Right now, when a message can't be processed, Service Bus quietly stashes it in a dead-letter sub-queue that nobody reads. Ten failed delivery attempts and then silence. The message isn't lost, exactly. It's just in the basement. Forever. Along with whatever else has piled up down there since we cut over to cloud.

That's not a system. That's a landfill with an API.

This doc lays out what we do instead: retry the stuff that deserves retrying, dead-letter the stuff that doesn't, write every dead-lettered message to a SQL table where a human being can actually look at it, and give operators a one-column-update path to replay the ones worth replaying. Plus a deliberate poison message we can fire into each channel to prove the whole pipeline works end to end.

## 2. What's in and what's out

In:

* Both queues (`dataflow-transform`, `api-add`) and both topics (`dataflow-transform-topic`, `api-add-topic` with `api-add-simple-sub`)
* The `DeadLetterRecord` table and its indexes
* One DLQ reader function per dead-letter sub-path
* The replay function
* New EventIds for the full lifecycle
* Poison triggers, one per channel
* Hooks into a runbook that lives in its own file

Out:

* Automatic replay. The schema leaves the door open. We're not walking through it yet.
* Cross-region anything
* Alert integrations (PagerDuty, ServiceNow). The EventIds will be ready. The wiring isn't our job this round.

## 3. Where we are today

Service Bus Premium, namespace `sbus-pe-2d99722c9843d8`, `rg-lab-sbus-01`, Canada Central. Every queue and topic comes with a `$DeadLetterQueue` sub-path baked in by Microsoft. Nobody reads from them. MaxDeliveryCount is the default 10 across the board. A poison message hits its tenth attempt, Service Bus moves it to the sub-queue, and it sits there until the heat death of the universe or someone finally notices, whichever comes first.

Fine for a sandbox. Embarrassing anywhere else.

## 4. How failures get classified

Four buckets. The bucket a message lands in tells the operator what to do with it and tells the (future) auto-replay engine whether it's allowed to touch it.

**Transient.** The handler threw because something blinked. SQL deadlock, HTTP timeout, 429 from an upstream, a packet got lost on the way to someone's router. These mostly die in Tier 1 before they ever see the dead-letter sub-queue. If one slips through anyway, it's tagged `Transient`. The only bucket auto-replay will ever be allowed to look at.

**DownstreamUnavailable.** A real outage, not a blink. Database was down for twenty minutes. Vendor API had an incident. You know it's this and not `Transient` because you see a hundred of them clustered in a five-minute window, all with the same error. After the dependency recovers, operators replay in bulk.

**SchemaViolation.** The payload is wrong. Missing fields, wrong types, CSV that doesn't parse, JSON with a typo. Replaying it will fail the same way every time because the bytes themselves are the problem. Fix it upstream or throw it out.

**Poison.** The payload is structurally fine but the handler hates it every time. Usually a logic bug you didn't know you had. Sometimes a test message we sent on purpose. Either way, don't replay. File a bug.

Anything the classifier can't place confidently goes in `Unknown` and gets escalated. Better to admit we don't know than to guess.

## 5. The three tiers of retry

### 5.1 Tier 1: retry in-process, before anyone notices

Polly. Exponential backoff with jitter. The handler's own code absorbs the blink and moves on.

Default policy for downstream I/O:

* 4 attempts (1 original + 3 retries)
* Delays of 200ms, 400ms, 800ms with decorrelated jitter
* Only retries on exceptions we've explicitly decided are transient (specific `SqlException` numbers, `HttpRequestException`, `TimeoutException`, the `TaskCanceledException` shapes that actually mean timeout)

Policies live in the shared infra project and get injected. No handler writes its own retry loop. I've seen what happens when every service writes its own, and the answer is "you get eight different retry loops, five of which are wrong."

### 5.2 Tier 2: Service Bus redelivery

Tier 1 gave up, handler threw, SB abandons the message and redelivers it. Up to MaxDeliveryCount times. Then it dead-letters with reason `MaxDeliveryCountExceeded`.

The count isn't uniform and shouldn't be:

| Channel | MaxDeliveryCount | Why |
|---|---|---|
| `dataflow-transform` | 5 | CSV in, parsed rows out. Deterministic. If it failed five times in a row, it's poison or schema, not flaky. Fail fast. |
| `api-add` | 10 | Real downstream dependencies, more legitimate blink surface. Give it room. |
| `dataflow-transform-topic` subs | 5 | Same reason as the queue. |
| `api-add-topic` / `api-add-simple-sub` | 10 | Same. |

Configured on the entities, not in code. You shouldn't need a redeploy to change a retry budget.

### 5.3 Tier 3: the DLQ and the operator

Once a message is in the dead-letter sub-queue, a reader function (one per sub-path, see Section 7) pulls it, classifies it, and writes a row to `DeadLetterRecord`. Then it completes the message on the sub-queue. The dead-letter sub-queue drains. The durable record of what died lives in SQL, where you can query it, sort it, stare at it, and eventually do something about it.

Replay is a human action. An operator flips `Status` to `PendingReplay`, the replay function picks it up on its next tick and re-publishes. No automation touches this path. Not yet. The schema supports auto-replay for `Transient` and `DownstreamUnavailable` without a table change, so when we build it later we won't have to migrate anything.

## 6. The table

### 6.1 `DeadLetterRecord`

Same database as `CommonLog`.

```sql
CREATE TABLE dbo.DeadLetterRecord (
    DeadLetterRecordId      BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_DeadLetterRecord PRIMARY KEY CLUSTERED,

    -- Who the message was
    MessageId               NVARCHAR(128)   NOT NULL,
    CorrelationId           NVARCHAR(128)   NULL,
    SessionId               NVARCHAR(128)   NULL,

    -- Where it came from
    SourceEntityType        NVARCHAR(16)    NOT NULL,  -- 'Queue' | 'Topic'
    SourceEntityName        NVARCHAR(260)   NOT NULL,
    SourceSubscriptionName  NVARCHAR(260)   NULL,

    -- What Service Bus had to say about it
    DeadLetterReason        NVARCHAR(260)   NULL,
    DeadLetterErrorDescription NVARCHAR(MAX) NULL,
    DeliveryCount           INT             NOT NULL,
    EnqueuedTimeUtc         DATETIME2(3)    NOT NULL,
    DeadLetteredTimeUtc     DATETIME2(3)    NOT NULL,

    -- What we decided it was
    FailureCategory         NVARCHAR(32)    NOT NULL
        CONSTRAINT CK_DeadLetterRecord_FailureCategory
        CHECK (FailureCategory IN
               ('Transient','DownstreamUnavailable','SchemaViolation','Poison','Unknown')),

    -- The body itself, so replay is actually possible
    MessageBody             NVARCHAR(MAX)   NOT NULL,
    MessageProperties       NVARCHAR(MAX)   NULL,      -- JSON
    ContentType             NVARCHAR(128)   NULL,

    -- What happens next
    Status                  NVARCHAR(32)    NOT NULL
        CONSTRAINT DF_DeadLetterRecord_Status DEFAULT ('New')
        CONSTRAINT CK_DeadLetterRecord_Status
        CHECK (Status IN
               ('New','UnderReview','PendingReplay','Replayed','Resolved','Discarded')),
    StatusUpdatedUtc        DATETIME2(3)    NOT NULL
        CONSTRAINT DF_DeadLetterRecord_StatusUpdatedUtc DEFAULT (SYSUTCDATETIME()),
    StatusUpdatedBy         NVARCHAR(128)   NULL,

    -- Replay scoreboard
    ReplayAttemptCount      INT             NOT NULL
        CONSTRAINT DF_DeadLetterRecord_ReplayAttemptCount DEFAULT (0),
    LastReplayAttemptUtc    DATETIME2(3)    NULL,
    LastReplayMessageId     NVARCHAR(128)   NULL,

    -- Operator scratchpad
    ResolutionNotes         NVARCHAR(MAX)   NULL,

    CreatedUtc              DATETIME2(3)    NOT NULL
        CONSTRAINT DF_DeadLetterRecord_CreatedUtc DEFAULT (SYSUTCDATETIME())
);
```

Why each field earns its keep:

* `MessageId` / `CorrelationId` / `SessionId`: so you can join this row back to `CommonLog` and reconstruct the whole story from "message published" to "message died."
* `SourceEntityType` / `SourceEntityName` / `SourceSubscriptionName`: the replay function needs to know exactly where to send the message back to. Topics get republished to the topic, never to a specific subscription.
* `DeadLetterReason` / `DeadLetterErrorDescription`: SB's own words. Primary input to the classifier.
* `FailureCategory`: what we decided. Different from `DeadLetterReason` on purpose.
* `MessageBody`: if this column is empty or wrong, replay is a fairy tale. Store it verbatim. Don't get clever.
* `MessageProperties`: everything else on the envelope, as JSON, so we can put the message back together exactly as it went out.
* `Status`: the one column an operator actually changes.
* `ReplayAttemptCount` and friends: so we can tell "replayed once, worked, done" apart from "we've replayed this eight times and it keeps dying, stop."

### 6.2 Indexes

```sql
CREATE INDEX IX_DeadLetterRecord_Status_StatusUpdatedUtc
    ON dbo.DeadLetterRecord (Status, StatusUpdatedUtc DESC);

CREATE INDEX IX_DeadLetterRecord_SourceEntityName_DeadLetteredTimeUtc
    ON dbo.DeadLetterRecord (SourceEntityName, DeadLetteredTimeUtc DESC);

CREATE INDEX IX_DeadLetterRecord_CorrelationId
    ON dbo.DeadLetterRecord (CorrelationId)
    WHERE CorrelationId IS NOT NULL;

CREATE INDEX IX_DeadLetterRecord_FailureCategory_DeadLetteredTimeUtc
    ON dbo.DeadLetterRecord (FailureCategory, DeadLetteredTimeUtc DESC);
```

Four indexes for the four ways operators will actually query this table. The operator dashboard (by status), per-channel triage (by source), correlation joins into `CommonLog`, category breakdowns for reports.

## 7. The reader functions

One reader per dead-letter sub-path. Four total.

| Function | Trigger |
|---|---|
| `DeadLetterReader_DataflowTransformQueue` | `dataflow-transform/$DeadLetterQueue` |
| `DeadLetterReader_ApiAddQueue` | `api-add/$DeadLetterQueue` |
| `DeadLetterReader_DataflowTransformTopic` | `dataflow-transform-topic/Subscriptions/<sub>/$DeadLetterQueue` |
| `DeadLetterReader_ApiAddTopicSimpleSub` | `api-add-topic/Subscriptions/api-add-simple-sub/$DeadLetterQueue` |

All four do the same thing:

1. Receive the dead-lettered message with its full envelope
2. Pull the SB metadata (`MessageId`, `CorrelationId`, delivery count, enqueued and dead-lettered times, the reason and description strings)
3. Classify per Section 4, based on the reason string and application properties
4. Serialize body and properties
5. Insert the row
6. Log two events (received, persisted) at the EventIds in Section 9
7. Complete the message on the dead-letter sub-queue

Readers are idempotent on `MessageId` + `SourceEntityName` + `DeadLetteredTimeUtc`. At-least-once delivery will sometimes hand us the same message twice. The second insert is a no-op guarded by an existence check.

If the insert fails, the reader abandons the dead-letter message. SB redelivers it. We end up reprocessing, which is fine. Losing the record entirely is not fine. I'd rather insert a row twice than lose one forever.

## 8. Replay

### 8.1 What the operator does

1. Queries `DeadLetterRecord` using the provided SQL. Filter by channel, time window, category, correlation ID, whatever.
2. Reads the payload and the error description. Decides.
3. If it's worth replaying: `Status = 'PendingReplay'`, fill in `StatusUpdatedBy`, drop a note in `ResolutionNotes` if there's anything useful to say.
4. If it's poison or a schema violation we can't fix upstream: `Status = 'Discarded'`, notes explaining why.

### 8.2 What the replay function does

Runs on a timer. Default 60 seconds.

1. Grab up to 25 rows where `Status = 'PendingReplay'`, oldest first
2. For each one, rebuild the SB message from `MessageBody`, `MessageProperties`, `ContentType`, keep the original `CorrelationId`
3. Publish to whatever `SourceEntityType` + `SourceEntityName` says. Topics go to the topic, not the subscription.
4. If it sent: `Status = 'Replayed'`, bump `ReplayAttemptCount`, record `LastReplayAttemptUtc` and the new message ID
5. If the publish itself threw: leave `Status` alone, bump the attempt count, record the time. Three strikes and the row goes to `UnderReview` with a log event loud enough to alert on.

The replay function never touches `MessageBody`. If the payload needs editing, that's an upstream conversation, not a DLQ conversation.

### 8.3 After replay

Message gets replayed, flows through, downstream is happy: operator sets `Status = 'Resolved'`. Message gets replayed and dies again: a fresh `DeadLetterRecord` row shows up with its own ID. You can follow the lineage via `CorrelationId` and `MessageId`. This is a feature. If something is dying on repeated replay, I want that to be visible as a pattern in the table, not hidden behind a mutated row.

## 9. EventIds

New entries in the `InfrastructureEvents` range. Actual numeric IDs assigned at implementation time so we don't collide with #53.

| Name | When | Level |
|---|---|---|
| `DeadLetterReaderStarted` | Reader host comes up | Information |
| `DeadLetterReaderStopped` | Reader host goes down | Information |
| `DeadLetterMessageReceived` | Reader got a message | Information |
| `DeadLetterRecordPersisted` | Row inserted | Information |
| `DeadLetterRecordPersistFailed` | Insert failed, abandoned back to SB | Error |
| `DeadLetterClassified` | Category assigned | Information |
| `DeadLetterClassificationUnknown` | Classifier punted to `Unknown` | Warning |
| `ReplayInitiated` | Replay function picked up a row | Information |
| `ReplaySucceeded` | Message re-published | Information |
| `ReplayFailed` | Publish threw | Warning |
| `ReplayExhausted` | Three failed attempts, escalated | Error |
| `ReplayFunctionStarted` | Replay host up | Information |
| `ReplayFunctionStopped` | Replay host down | Information |

Every event carries `CorrelationId` and, when they apply, `DeadLetterRecordId`, `SourceEntityName`, and `FailureCategory` as structured props. All of it lands in `CommonLog` through the existing sink. The DLQ queries in Section 11 pull from it.

## 10. Poison on purpose

Two triggers, one per channel. Both use the `POISON_` prefix so anyone looking at a DLQ record instantly knows they're seeing a test message, not a real failure.

No synthetic detection. No new `if (message.isPoison)` branch. The triggers exercise the real validator paths that would reject any malformed real-world payload the same way. If we ever remove the validators, these tests break, and they should.

### 10.1 `dataflow-transform` poison

A CSV row with `POISON_FORCE_DEADLETTER` in the GL Account column. The existing validator rejects it for the same reason it rejects any other garbage that doesn't match the GL Account regex. Real code path, real throw.

### 10.2 `api-add` poison

An API request with a correlation ID of `POISON_FORCE_DEADLETTER_<guid>`. The handler's validator rejects any correlation ID that doesn't match our standard format. Same principle.

### 10.3 What the tests prove

One integration test per trigger. Each one verifies:

1. The message was published
2. After MaxDeliveryCount attempts, it shows up in `DeadLetterRecord` with the category we expect
3. `CorrelationId` survived the trip
4. `MessageBody` survived the trip
5. Setting `Status = 'PendingReplay'` results in a replay, and since the payload is still poison, a second `DeadLetterRecord` row appears

That's the hackathon demo. Push a poison CSV row, watch the table populate, set the status, watch it try again, watch it die again, look at both rows. If that works, the whole mechanism works.

## 11. Runbook and queries

The runbook lives in `docs/runbooks/dlq-incident-response.md` and covers on-call triage, how to tell a transient spike from a real outage, how to find a publisher sending malformed payloads, a replay decision tree per category, escalation, and notes on each of the queries below.

Queries live in `docs/queries/deadletter/`, matching the convention from `docs/queries/commonlog/`:

1. `01-recent-records.sql` — newest N across all channels
2. `02-records-by-channel.sql` — one channel at a time
3. `03-records-by-correlation.sql` — join to `CommonLog`
4. `04-pending-replay.sql` — the operator's work queue
5. `05-category-breakdown.sql` — what's dying and how much
6. `06-replay-outcomes.sql` — did replay actually help
7. `07-escalations.sql` — `UnderReview` with high attempt counts, i.e. the stuff that needs a human now
8. `08-time-range.sql` — free-form window with all filters

## 12. What we're not solving yet

Nothing in this list blocks the first build. All of it's fair game once the pipeline's live.

* Auto-replay for `Transient` and `DownstreamUnavailable`. Trigger, policy, guardrails, the whole conversation.
* Retention on `DeadLetterRecord`. 90 days hot, archive beyond, probably. Not today.
* Batch inserts on the readers if volume ever justifies it. Right now one-at-a-time is fine.
* A UI for operators. SQL is the interface in version one.

## 13. Order of operations

1. `DeadLetterRecord` DDL and indexes as a dacpac-ready script
2. Classification helper and the new EventIds
3. First reader end to end (`dataflow-transform` queue) with unit tests
4. Other three readers
5. Replay function
6. Poison triggers and their integration tests
7. SQL queries
8. Runbook
9. MaxDeliveryCount tweaks on the SB entities

Each one its own commit. Each commit message says what, why, and how it was verified. Same pattern as the rest of this branch.
