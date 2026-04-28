# DLQ Incident Response Runbook

This is the runbook for FileIt dead-letter queue activity. If you are on-call and have been paged because a `DeadLetterRecord` row appeared, a stale-pending threshold was breached, or a downstream system is reporting unexpected message volume, **start here**.

This document is companion to:
- `docs/dead-letter-strategy.md` (design and architecture)
- `docs/queries/deadletters/` (the SQL queries this runbook references by number)

## Decision tree

Page received
|
v
Is the system on fire? (production outage, P0/P1)
|  Yes -> escalate first, triage second. Do not start with this runbook.
|  No  -> continue
v
Open the dead-letter inbox: docs/queries/deadletters/01-pending-inbox.sql
|
v
Is there exactly one new record?         <- single-record incident
|  Yes -> go to "Single record triage"
|  No  -> continue
v
Are records clustered by SourceEntityName + DeadLetteredTimeUtc within ~5 minutes?  <- mass-failure incident
|  Yes -> go to "Mass failure triage"
|  No  -> go to "Mixed-bag triage" (rare; usually means multiple unrelated issues converged)

## Single-record triage

1. **Open the record's full detail and upstream story.**
   Run `docs/queries/deadletters/02-record-detail-with-timeline.sql` with the record id.
   You'll get two result sets: the dead-letter record itself, and the upstream `CommonLog` timeline for the same `CorrelationId`. The timeline is what the upstream flow logged before the message failed five times. **Read it before doing anything else.** Three minutes of reading saves an hour of debugging.

2. **Identify the FailureCategory and act per the table below.**
   The classifier has already told you what kind of failure this is. Trust the category for first action; verify it for second action.

   | Category | First action | If first action insufficient |
   |---|---|---|
   | `Transient` | Wait 5 minutes. Re-check status. If still `New`, set to `PendingReplay` and let the timer pick it up. | If replay also fails, escalate `FailureCategory` to `UnderReview` and dig into upstream dependency health. |
   | `DownstreamUnavailable` | Confirm the downstream is healthy now (check its own dashboards, ping its team). Do NOT replay until you have positive confirmation. | If downstream is still degraded, leave at `New`. If a backlog accumulates, batch-replay with the timer once downstream recovers. |
   | `SchemaViolation` | Open the original payload in `MessageBody`. Identify the malformed field. Trace the publisher (via `SourceEntityName` and the upstream timeline). **Fix the publisher**, not the record. | Discard the record once the publisher is fixed; the corrupt payload should not be replayed. |
   | `Poison` | Inspect the payload. If it's the deliberate demo trigger (`POISON_` prefix), discard with a note saying "demo trigger". If it's a real Poison, the handler has a bug; file an engineering ticket. | Do NOT replay a real Poison until the handler is fixed; the replay will just re-poison. |
   | `Unknown` | Read the `ResolutionNotes` (the classifier's reasoning). Decide manually which of the above categories this most resembles, then follow that workflow. | If genuinely ambiguous, escalate to engineering with a link to the record. |

3. **Promote the record's status as you act.**
   Lifecycle states are operational signals, not just bookkeeping. Update them as decisions are made:

```sql
   -- Move New to UnderReview when you start triaging:
   UPDATE [dbo].[DeadLetterRecord]
   SET [Status] = N'UnderReview',
       StatusUpdatedUtc = SYSUTCDATETIME(),
       StatusUpdatedBy = N'<your-name-or-handle>',
       ResolutionNotes = ResolutionNotes + N' | Triage started by <your-name>'
   WHERE DeadLetterRecordId = <id>;

   -- Promote to PendingReplay when you have decided to replay:
   UPDATE [dbo].[DeadLetterRecord]
   SET [Status] = N'PendingReplay',
       StatusUpdatedUtc = SYSUTCDATETIME(),
       StatusUpdatedBy = N'<your-name-or-handle>',
       ResolutionNotes = ResolutionNotes + N' | Approved for replay by <your-name>: <one-line-reason>'
   WHERE DeadLetterRecordId = <id>;

   -- Discard a record that should never replay:
   UPDATE [dbo].[DeadLetterRecord]
   SET [Status] = N'Discarded',
       StatusUpdatedUtc = SYSUTCDATETIME(),
       StatusUpdatedBy = N'<your-name-or-handle>',
       ResolutionNotes = ResolutionNotes + N' | Discarded by <your-name>: <one-line-reason>'
   WHERE DeadLetterRecordId = <id>;
```

4. **If you need replay to happen RIGHT NOW (do not wait for the timer):**

```bash
   curl -X POST \
     -H "x-functions-key: <your-key>" \
     -H "X-Initiated-By: <your-name>" \
     "https://<services-host-url>/api/deadletter/<id>/replay"
```

   The HTTP response codes follow REST conventions; see `DeadLetterReplayHttp` for the mapping. 200 means sent, 409 means the record was not in `PendingReplay` (race or already advanced), 422 means structurally invalid, 502 means broker rejected the send.

5. **Resolve when the original failure cause is fixed.**
   `Replayed` means we re-published. It does NOT mean downstream succeeded. Verify the downstream succeeded (typically by querying `DataFlowRequestLog` or whatever the downstream side persists), then advance the record to `Resolved`:

```sql
   UPDATE [dbo].[DeadLetterRecord]
   SET [Status] = N'Resolved',
       StatusUpdatedUtc = SYSUTCDATETIME(),
       StatusUpdatedBy = N'<your-name-or-handle>',
       ResolutionNotes = ResolutionNotes + N' | Verified downstream success by <your-name>'
   WHERE DeadLetterRecordId = <id>;
```

## Mass failure triage

A cluster of records dead-lettered in a narrow time window with the same `SourceEntityName` is almost always a downstream incident, not five independent bad messages.

1. **Stop. Do not replay anything yet.** Bulk replay against a still-broken downstream just produces a second wave of dead letters and obscures the original incident.

2. **Quantify the cluster.** Run `docs/queries/deadletters/03-by-source-and-category.sql`. Look for:
   - High `RecordCount` concentrated on one `SourceEntityName`.
   - High `RecordCount` of `DownstreamUnavailable` or `Transient`. (Spike of `SchemaViolation` is a different incident: an upstream contract changed.)
   - `OldestDeadLetterUtc` and `NewestDeadLetterUtc` close together (cluster) versus spread out (steady-state degradation).

3. **Identify and confirm the downstream.** `SourceEntityName` tells you which queue/topic the messages were on. Map that to the downstream consumer:

   | SourceEntityName | Consumer host | Likely downstream dependencies |
   |---|---|---|
   | `dataflow-transform` | `FileIt.Module.DataFlow.Host` | Azure Blob Storage (input/output containers), Azure SQL (`DataFlowRequestLog`) |
   | `api-add` | `FileIt.Module.Services.Host` | Azure SQL (`ApiLog`), the simulated downstream API |
   | `api-add-topic` (subscription `api-add-simple-sub`) | `FileIt.Module.SimpleFlow.Host` | Azure Blob Storage, Azure SQL (`SimpleRequestLog`) |

4. **Check the downstream's own health.** Don't speculate; look at:
   - Its CommonLog stream (filter by Application = the consumer host) for the same time window.
   - Its actual outcome tables (`DataFlowRequestLog.Status`, `SimpleRequestLog.Status`).
   - The actual Azure resource: blob container reachable? SQL server responsive? Service Bus namespace healthy?

5. **Fix the downstream first. Replay second.** Once you have positive confirmation the downstream is recovered (not just "should be working"), batch-promote the cluster:

```sql
   -- Promote an entire cluster to PendingReplay. Be specific in the WHERE clause;
   -- a too-broad UPDATE replays things that should not be replayed.
   UPDATE [dbo].[DeadLetterRecord]
   SET [Status] = N'PendingReplay',
       StatusUpdatedUtc = SYSUTCDATETIME(),
       StatusUpdatedBy = N'<your-name-or-handle>',
       ResolutionNotes = ResolutionNotes
           + N' | Mass replay after <downstream-name> recovery, ticket <link>'
   WHERE [Status] IN (N'New', N'UnderReview')
     AND SourceEntityName = N'<the-affected-channel>'
     AND FailureCategory IN (N'Transient', N'DownstreamUnavailable')
     AND DeadLetteredTimeUtc BETWEEN N'<incident-start-utc>' AND N'<incident-end-utc>';
```

   The timer (every 5 minutes, 25 records per tick, see `DeadLetterReplayTimer`) drains the backlog. For very large clusters, monitor `docs/queries/deadletters/05-replay-history.sql` to confirm replays are succeeding before promoting more.

## Mixed-bag triage

Multiple unrelated dead letters at once. Rare. Treat each cluster independently using the workflows above. Do NOT batch across categories or sources unless you can articulate exactly why the same action is correct for all of them.

## Stale pending alerts

If the alert source is "stale pending" (records that have been `New` or `UnderReview` for longer than threshold), run `docs/queries/deadletters/04-stale-pending.sql` and treat the result like a single-record or mass-failure case as appropriate. The point of the stale alert is to catch dead letters the on-call missed; treat it as a process-failure signal as well as an operational one.

## Known operational hazards

- **Do not run blind UPDATE statements without a WHERE clause** that includes both `SourceEntityName` and a time bound. A typo here will replay every dead letter in the table and cause a thundering herd.
- **Do not replay `Poison` or `SchemaViolation` records without a code or upstream fix.** They will re-poison and re-fail. Discard them once the upstream is corrected.
- **Do not assume `Replayed` means success.** It means we re-published. The downstream still has to succeed; verify before advancing to `Resolved`.
- **Do not bypass the lifecycle.** It exists so the audit trail is honest. If you find yourself wanting to set `[Status] = N'Resolved'` directly without ever passing through `PendingReplay` or `Replayed`, ask whether you actually want `Discarded` (no replay needed, work was abandoned).
- **The HTTP replay endpoint is per-record only.** Bulk replay via curl is not supported by design. Bulk replay is what the SQL `UPDATE ... SET Status = N'PendingReplay'` is for; the timer drains the backlog.

## Demo / training procedure

To demonstrate the entire DLQ pipeline end-to-end (useful for onboarding, hackathon demos, or sanity-checking after deploys):

1. Start with a known-good `GLAccount.csv`.
2. Edit one row so its `COMPANYCODE` value starts with `POISON_` (e.g., `POISON_DEMO123`). See `TransformGlAccounts.PoisonCompanyCodePrefix` for the contract.
3. Drop the modified CSV into the `dataflow-source` blob container.
4. Watch the flow attempt processing. The transform throws on the poison row. Service Bus retries 5 times.
5. After ~30-60 seconds, the message is dead-lettered. `DataFlowDeadLetterReader` fires. A `DeadLetterRecord` row appears with `Status='New'`, `FailureCategory='Poison'`, `MessageBody` containing the poisoned CSV.
6. Open the inbox query (`01-pending-inbox.sql`). The new record is at the top.
7. Open the detail query (`02-record-detail-with-timeline.sql`) with that record id. Read the timeline.
8. (Optional) Walk through a replay: edit the CSV in the blob to remove the `POISON_` prefix, promote the record to `PendingReplay`, hit the HTTP replay endpoint, watch the message land back in `dataflow-transform` and process successfully.
9. Resolve.

This procedure is the canonical smoke test for the DLQ pipeline. Run it after any change to the dead-letter code paths to confirm the pipeline still works end-to-end.

## Escalation

If at any point you are unsure which workflow applies, the system is behaving in ways this document does not describe, or the dead-letter rate is climbing faster than you can triage:

1. Page the engineering on-call.
2. Capture (do not lose) the time window, the affected `SourceEntityName`(s), and a representative `DeadLetterRecordId`.
3. Do NOT bulk-discard or bulk-replay during an active incident.

The dead letters will still be there in the morning; they are persistent by design. Better to leave the queue full and pause for fresh eyes than to clear it incorrectly.