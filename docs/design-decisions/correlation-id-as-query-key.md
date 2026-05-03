# CorrelationId as a Query Key

**Issue:** #8 (Pr0x1mo mirror)
**Status:** Resolved by audit
**Last updated:** 2026-05-03
**Owner:** Proximus (Xavier Borja)

## 1. The Original Concern

Issue #8 raises a hypothetical performance concern with this exact framing:

> This system uses a GUID Correlation ID to correlate all log statements related to a flow. These log statements might come from the flow Function App or the Common App. We also have a RequestLog table that we updated based on CorrelationID. That may not be efficient. We may also want to pass around the RequestLog ID as well, which will be the PK. That would be efficient. Test this.

Restated: GUID-based lookups against a non-clustered index are slower than int-PK-based lookups against a clustered index. The issue proposes carrying the RequestLog primary key alongside the CorrelationId in the message envelope, so consumers can update by PK instead of by GUID.

## 2. Audit: Does the Concern Apply to the Actual Implementation?

The issue was filed against an early version of the repository where the code surface was minimal. The current implementation was built out as part of the hackathon work, so the relevant question is: in the code that exists today, where is CorrelationId used as a query predicate, and is any of it the kind of operation #8 is worried about?

The audit was a single grep across the production Infrastructure project for any line referencing `CorrelationId` in a query-shaped context (Where, FirstOrDefault, Single, Find, FromSql, HasIndex):

```
Get-ChildItem ... -Filter "*.cs" -Recurse
  | Select-String -Pattern "CorrelationId" -Context 1,1
  | Where-Object { ...Where|FirstOrDefault|Single|Find|FromSql|HasIndex... }
```

Result: exactly one hit, in `FileIt.Infrastructure/Data/CommonLogRepo.cs`:

```csharp
public async Task<CommonLog?> GetByClientRequestIdAsync(string? clientRequestId)
{
    using var dbContext = Factory.CreateDbContext();
    return await dbContext.CommonLogs.FirstOrDefaultAsync(log =>
        log.CorrelationId == clientRequestId && log.Environment == this.config.Environment
    );
}
```

That is the entire surface of CorrelationId-as-query-key in the production code.

## 3. Why This Is Not the Case #8 Describes

The issue calls out an *update* path, against a *RequestLog* table, in a *hot* code path. The actual code has none of those characteristics:

* **It is a SELECT, not an UPDATE.** The cost concern in #8 was about UPDATE statements that translate CorrelationId to a row before mutating it. There are zero UPDATE-by-CorrelationId paths in the production Infrastructure project today.

* **It targets CommonLog, not a RequestLog table.** CommonLog is the audit and diagnostic log written by the Serilog `DatabaseSink`. It is intentionally keyed by CorrelationId because the entire point of an audit log is to retrieve entries by their correlation key after the fact. There is no upstream PK to forward to a CommonLog reader, because the reader is typically a human operator running a diagnostic query, not a downstream message consumer.

* **It is a debugging and correlation tool, not a hot path.** `GetByClientRequestIdAsync` is called from operator-facing tooling and from the dead-letter classifier when it needs to attach related log context. It is not invoked on every message receive, it is not on the request-handling critical path, and its latency is invisible to end users.

* **The repos that DO update logs (ApiLogRepo, SimpleRequestLogRepo, DataFlowRequestLogRepo) all update by primary key already.** Each repo inherits `BaseRepository<T>.UpdateAsync(entity)`, which receives a fully tracked or attached entity (with its PK already populated) and writes by PK. These repos never look up an entity by CorrelationId in order to update it. Cesar's worry already does not apply, structurally.

So the framework in #8 maps onto a code shape that doesn't exist here. The conclusion of the audit is: **the concern raised in #8 is not a live concern for this implementation. No envelope change is needed. No optimization is needed. The CorrelationId-as-query-key usage that does exist is in the right place for the right reason.**

## 4. The One CorrelationId Query, Examined on Its Own Merits

Setting aside #8's framing, is `CommonLogRepo.GetByClientRequestIdAsync` itself well designed? Three observations.

### 4.1 The Compound Predicate Suggests a Compound Index

The method filters on both `CorrelationId` AND `Environment`. A non-clustered index on `CorrelationId` alone would still seek correctly, but every match would then require an additional Environment check, and the index would not be uniquely selective until both columns matched.

The optimal index for this query is a compound index `(Environment, CorrelationId)` because:

* `Environment` has very low cardinality (a handful of values: LocalDev, Dev, UAT, Prod) which makes it a good leading column for partitioning. Index pages stay aligned to environment boundaries, which is also useful for any future per-environment retention or archival policy.
* `CorrelationId` is the high-cardinality second column where the seek actually narrows down to a row.

This is a known SQL Server pattern: lead a compound index with the lower-cardinality, more-frequently-filtered column. The current schema may or may not have this index already; verifying or adding it is a separate, low-risk improvement that does NOT require the envelope change #8 proposed.

### 4.2 Parameter Naming is Misleading

The method takes `string? clientRequestId` and compares it to `log.CorrelationId`. Reading the method in isolation, this looks like a bug. It is not a bug; it reflects the fact that the system uses `ClientRequestId` (the value the API caller submitted) and `CorrelationId` (the value propagated through internal messages) as the same logical identifier at different layer boundaries. The naming asymmetry is a layer-boundary translation, not a defect.

A small clarifying refactor would be to rename the parameter to `correlationId` and let the caller (which presumably knows the value as `clientRequestId`) do the assignment at its own boundary. This is cosmetic and would not affect performance. Filed for the future-work list, not for this issue.

### 4.3 The `using` Pattern is Correct

The method opens a fresh `DbContext` per call via the factory and disposes it deterministically. This matches the per-call DbContext discipline the rest of the data layer follows and is the right choice for a stateless repository. No change needed.

## 5. Threshold For Revisiting

This audit closes #8 against the current code shape, but the conclusion is conditional. The recommendation could change if any of the following happens:

* **CommonLog grows to 100M+ rows.** At that scale the GUID-vs-PK seek-cost difference starts to compound, and even a non-hot-path query can become a noticeable operator-experience problem. The mitigation at that point is not the envelope change #8 proposed; it is partitioning CommonLog on `(Environment, CreatedOn)` and adding the compound index from §4.1 if it is not already in place.

* **A new path is introduced that updates a row by CorrelationId.** This would be a structural change worth pushing back on at code review, because the existing pattern of "carry the PK on the entity, update by PK" works without any envelope change. If a future module needs to look up a row by CorrelationId because it doesn't have the PK at hand, the right fix is to ensure the upstream message carries the PK as an optional `X-FileIt-EntityId` application property, not to retrofit a slow GUID-based update path.

* **The dead-letter replay service starts looking up CommonLog by CorrelationId on every replay.** Today the replay service does not do this, but if it ever did, the lookup would be on the hot path of incident recovery and would deserve a benchmark of its own. As of this writing, replay correlates via `DeadLetterRecord` (which has its own PK) and does not query CommonLog.

## 6. Engineering Observation Filed Separately

The compound index recommendation in §4.1 is worth a small follow-up issue independent of #8. It is a one-line schema change with a pure win and no design risk. Filed as a candidate for the next DACPAC update.

## 7. Conclusion

Issue #8 asked: "we update RequestLog by CorrelationId, is that efficient, should we pass the PK around?"

The audit answers: **the implementation does not update by CorrelationId. There is no RequestLog table being updated by CorrelationId. The one CorrelationId-as-query-key usage that exists is a SELECT against CommonLog, used for diagnostics, off the hot path, and is the correct design for that role. No envelope change is needed. No benchmark would produce actionable data because there is no production code path matching the issue's framing to benchmark.**

This is a closure by design audit, not by performance measurement. Mit-route closure: the question was answered with the most direct evidence (the actual code), and the answer is that the question doesn't apply.

If at a future time the conditions in §5 change, this document should be revisited and the recommendation re-evaluated against the new shape of the code.

## 8. Change Log

| Version | Date | Author | Notes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-05-03 | Proximus | Initial audit and writeup, issue #8. |
