# FileIt Message Data Standards

**Issue:** #14 (Pr0x1mo mirror)
**Status:** Draft for review
**Last updated:** 2026-05-03
**Owner:** Proximus (Xavier Borja)

## 1. Purpose

This document defines the contract that every FileIt module follows when publishing messages to Azure Service Bus. The goal is interoperability across modules without each producer or consumer reinventing envelope conventions, and to give the dead letter diagnostic pipeline (#22) a stable surface to read from.

If a module publishes a message that does not conform to this standard, downstream consumers (including the DLQ reader) cannot correlate, deduplicate, or compute true message age.

## 2. Scope

Applies to:
* All queues in the FileIt Service Bus namespace `sbus-pe-2d99722c9843d8` (dataflow-transform, api-add, secure-queue, future module queues).
* All topics and subscriptions (dataflow-transform-topic, api-add-topic, api-add-simple-sub).
* The dead letter sub-queue of every entity above.

Does not apply to:
* HTTP request bodies on Functions HTTP triggers.
* Database row payloads (Common.Log, DeadLetterRecord, etc.).
* Outbound API payloads to ComplexApi or other external services. Those have their own contracts.

## 3. The Envelope

A FileIt message has two layers:

1. **Service Bus native properties.** MessageId, CorrelationId, Subject, ContentType, ApplicationProperties. Set on the `ServiceBusMessage` object directly.
2. **JSON body.** Carries the typed business payload, with a small `envelope` block at the root for schema identification.

Producers MUST populate every required field below. Consumers MAY assume required fields are present and MUST treat absence as a malformed message (route to DLQ with reason `envelope-violation`).

### 3.1 Service Bus Native Properties

| Property | Required | Type | Notes |
| :--- | :---: | :--- | :--- |
| MessageId | Yes | UUID v4 | One per physical message. Used for Service Bus deduplication when enabled on the entity. |
| CorrelationId | Yes | UUID v4 | One per logical operation. Propagated unchanged from upstream message. See §5. |
| Subject | Yes | string | High level intent (event name). See §6. |
| ContentType | Yes | string | Always `application/json; charset=utf-8`. |
| Body | Yes | bytes | UTF 8 JSON. See §4. |
| To, ReplyTo, ReplyToSessionId, SessionId | Optional | string | Use only when the entity is session enabled. No FileIt entity uses sessions today. |

### 3.2 Application Properties: the `X-FileIt-*` Namespace

All FileIt specific metadata lives in `ApplicationProperties` under the `X-FileIt-*` prefix. The constants are defined in `FileIt.Infrastructure/Messaging/FileItMessageProperties.cs`.

| Property | Required | Type | Notes |
| :--- | :---: | :--- | :--- |
| `X-FileIt-EnqueuedTimeUtc` | Yes | ISO 8601 string | Set at every publish. Preserves true original publish time across retries, DLQ replays, and forwards. See §7. |
| `X-FileIt-Source` | Yes | string | Module that produced the message. Examples: `DataFlow.App`, `SimpleFlow.App`, `Services.App`. |
| `X-FileIt-EnvelopeVersion` | Yes | int | Currently `1`. Bumped on breaking schema changes. See §8. |
| `X-FileIt-AttemptCount` | No | int | Set by retry middleware on republish. Absent on first publish. |
| `X-FileIt-DeadLetterReason` | No | string | Set only by middleware that routes to DLQ, or by the DLQ replay tool. See §11. |

Application properties outside the `X-FileIt-*` namespace are reserved for SDK managed properties (`Diagnostic-Id`, `traceparent`, etc.) and MUST NOT be repurposed.

## 4. Body Schema Conventions

The body is always UTF 8 JSON. Every body has the following root shape:

```json
{
  "envelope": {
    "version": 1,
    "schema": "fileit.dataflow.transform.v1"
  },
  "payload": {
  }
}
```

`envelope.schema` follows the convention `fileit.<module>.<event>.v<n>`. The `<n>` matches `X-FileIt-EnvelopeVersion`. The schema string is the canonical identifier for body shape; consumers SHOULD switch on it rather than on `Subject` when selecting a deserializer.

`payload` is the typed business payload. Each module defines its own DTO record types in `<Module>.Domain/Messages/`. Required vs optional fields are documented on those DTOs via XML doc comments and `[JsonPropertyName]` / `[JsonRequired]` attributes.

JSON serialization rules:
* camelCase property names.
* ISO 8601 UTC for all DateTime values, with explicit `Z` suffix.
* UUIDs serialize as plain string (no braces, no urn prefix).
* Decimals serialize as string when precision matters (currency, rates), as number otherwise.
* Null values serialized as `null`, not omitted, unless the field is explicitly optional with `[JsonIgnore(Condition = WhenWritingNull)]`.

## 5. Identifiers and Idempotency

Two distinct UUIDs travel with every message. They serve different purposes and MUST NOT be conflated.

### 5.1 MessageId

`MessageId` is unique per physical message. A retry of the same logical operation produces a NEW MessageId.

Used for:
* Service Bus native deduplication, when the entity has `RequiresDuplicateDetection = true` and a deduplication window configured.
* Correlation in the broker's own diagnostic logs.

NOT used for: consumer side idempotency. Use CorrelationId for that (see §5.3).

### 5.2 CorrelationId

`CorrelationId` is unique per logical operation. A retry, a fan out, or a downstream message produced as a side effect of the original operation, all carry the SAME CorrelationId.

Used for:
* Tracing across modules. A single `AddRequested` operation may produce a chain of messages with the same CorrelationId across SimpleFlow, DataFlow, Services, and ComplexApi.
* Consumer side idempotency at the operation level.
* Joining log lines in CommonLog (which carries CorrelationId as an indexed column, see #41).

### 5.3 The Idempotency via Correlation Pattern

Consumers SHOULD implement the following pattern for at least once semantics:

1. Receive message.
2. Check the consumer's idempotency table for `CorrelationId`.
3. If present and status is `Completed`, ack the message and return (no side effect).
4. If present and status is `InProgress` and started within the broker's lock window, abandon the message (let the original handler finish or expire).
5. If absent, insert a row with status `InProgress`, perform the side effect, update status to `Completed`, then ack.
6. On exception after the side effect commits, update to `Completed` then rethrow only if the side effect was idempotent at the resource level. Otherwise update to `Failed` and rethrow.

The idempotency table is per consumer, not shared across modules. Suggested schema:

```sql
CREATE TABLE Idempotency (
  CorrelationId  uniqueidentifier NOT NULL PRIMARY KEY,
  Status         varchar(20)      NOT NULL,
  StartedUtc     datetime2(3)     NOT NULL,
  CompletedUtc   datetime2(3)     NULL,
  ResultHash     varbinary(32)    NULL,
  CONSTRAINT CK_Idempotency_Status
    CHECK (Status = 'InProgress' OR Status = 'Completed' OR Status = 'Failed')
);

CREATE INDEX IX_Idempotency_StartedUtc ON Idempotency (StartedUtc);
```

CHECK constraint uses an OR chain in catalog order, not an `IN (...)` list. This matches the #3 DACPAC convention so that `sqlpackage` does not reorder the predicate on every publish.

`ResultHash` is optional. When present it allows detection of `idempotency-conflict` (same CorrelationId, different payload), which is rare but worth catching.

## 6. Subject Naming and Routing

`Subject` follows the convention `<Aggregate><PastTenseEvent>` for events:
* `AddRequested`
* `TransformCompleted`
* `TransformFailed`
* `RecordPersisted`
* `RecordRejected`

PascalCase, no separators, past tense for events that have happened. For commands (rare; prefer events), use imperative: `Transform`, `Persist`. Commands are pull style (one consumer expected); events are publish style (zero or more consumers).

Subscription filters SHOULD filter on `Subject`, not on body content. Body filters require server side deserialization and are slow.

Example subscription rule that only wants completion events:

```sql
sys.Subject LIKE '%Completed' OR sys.Subject LIKE '%Persisted'
```

## 7. Time Tracking: Why `X-FileIt-EnqueuedTimeUtc` Exists

Service Bus has a built in `EnqueuedTimeUtc` system property. It records when the broker accepted the message into the entity. This is fine until any of the following happens:

1. The message is abandoned and redelivered. `EnqueuedTimeUtc` does not change, but the entity has accumulated wait time the property does not reflect.
2. The message is dead lettered, then later replayed from DLQ. The replayed copy has a NEW `EnqueuedTimeUtc` reflecting the DLQ replay, not the original publish.
3. The message is forwarded between entities (auto forwarding). The destination's `EnqueuedTimeUtc` reflects the forward, not the original publish.

`X-FileIt-EnqueuedTimeUtc` is set by the producer at the original publish and propagated unchanged through retries, DLQ replays, and forwards. The DLQ diagnostic pipeline from #22 uses this property to compute true age at failure as `now - X-FileIt-EnqueuedTimeUtc`, which is the metric that matters for SLO reporting.

Format: ISO 8601 with explicit UTC offset. Example: `2026-05-03T14:32:11.482Z`. Producers SHOULD use `DateTime.UtcNow.ToString("O")` (the round trip format) which emits this exact shape.

The constant key is `FileItMessageProperties.EnqueuedTimeUtcKey`.

## 8. Versioning

`X-FileIt-EnvelopeVersion` and `envelope.version` MUST match.

Breaking changes (new required field, removed field, semantic change of an existing field) bump the version. Additive changes (new optional field, new optional application property) do not.

Producers MAY publish multiple versions in parallel during a migration window. Consumers MUST handle every version they have ever seen until the producer confirms the old version is retired. Version retirement is announced by the producer module owner via a PR that updates this document.

## 9. Producer Checklist

Before any module publishes a message, verify:

* MessageId is a fresh UUID v4 (`Guid.NewGuid().ToString()`).
* CorrelationId is propagated from the upstream message, or freshly generated for top level operations.
* Subject matches the §6 convention.
* ContentType is exactly `application/json; charset=utf-8`.
* Body is valid UTF 8 JSON with `envelope` and `payload` at the root.
* `X-FileIt-EnqueuedTimeUtc` is set to `DateTime.UtcNow.ToString("O")`.
* `X-FileIt-Source` identifies the producing module.
* `X-FileIt-EnvelopeVersion` matches `envelope.version`.

The `MessagePublisher` helper in `FileIt.Infrastructure/Messaging/` enforces these checks. Direct `ServiceBusSender.SendMessageAsync` calls are discouraged outside of test harnesses.

## 10. Consumer Checklist

Before any module processes a message, verify:

* Required envelope properties are present (route to DLQ with reason `envelope-violation` if not).
* `X-FileIt-EnvelopeVersion` is supported by the current consumer (route to DLQ with reason `unsupported-version` if not).
* CorrelationId has not already been processed (idempotency check from §5.3).
* Body deserializes against the expected DTO (route to DLQ with reason `body-violation` if not).

The `MessageReceiver` middleware in `FileIt.Infrastructure/Messaging/` performs the first three checks. Body deserialization is the consumer's responsibility, but the middleware will translate `JsonException` into a `body-violation` DLQ route automatically.

Note: the `ExceptionHandlingMiddleware` fix from #22 ensures non HTTP triggers rethrow exceptions, so unhandled exceptions in consumers correctly drive the delivery counter and eventually route to DLQ with reason `unhandled-exception` after `MaxDeliveryCount` is exhausted.

## 11. Dead Letter Conventions

When a message is moved to DLQ, the following native properties are set by Service Bus or by the middleware:

* `DeadLetterReason` (native): a short tag.
* `DeadLetterErrorDescription` (native): the exception text or rejection reason.

Plus FileIt specific:

* `X-FileIt-DeadLetterReason`: same as native, kept for cross broker portability.
* `X-FileIt-AttemptCount`: total delivery attempts before DLQ.
* `X-FileIt-EnqueuedTimeUtc`: preserved from the original publish (NOT reset).

Standard reasons:

| Reason | When |
| :--- | :--- |
| `envelope-violation` | A required Service Bus property is missing or malformed. |
| `body-violation` | Body cannot be deserialized to the expected DTO. |
| `unsupported-version` | EnvelopeVersion outside the consumer's supported range. |
| `unhandled-exception` | Consumer threw, MaxDeliveryCount exhausted. Set by middleware (see #22). |
| `idempotency-conflict` | Same CorrelationId seen with a different payload hash. Rare. |

The DLQ reader from #22 reads `X-FileIt-DeadLetterReason` and `X-FileIt-EnqueuedTimeUtc` to populate `DeadLetterRecord` rows. The schema is defined at `FileIt.Database\Tables\DeadLetterRecord.sql` (24 columns, 6 indexes; see the #22 verification notes for the manual deploy step until the build picks up that DDL).

## 12. Examples

### 12.1 DataFlow Module Publishing TransformCompleted

```csharp
var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(new
{
    envelope = new { version = 1, schema = "fileit.dataflow.transform.v1" },
    payload = new
    {
        sourceFileId = sourceId,
        rowsProcessed = 4827,
        outputBlobUri = blobUri.ToString()
    }
}))
{
    MessageId     = Guid.NewGuid().ToString(),
    CorrelationId = inboundCorrelationId.ToString(),
    Subject       = "TransformCompleted",
    ContentType   = "application/json; charset=utf-8"
};

message.ApplicationProperties[FileItMessageProperties.EnqueuedTimeUtcKey]   = DateTime.UtcNow.ToString("O");
message.ApplicationProperties[FileItMessageProperties.SourceKey]            = "DataFlow.App";
message.ApplicationProperties[FileItMessageProperties.EnvelopeVersionKey]   = 1;

await sender.SendMessageAsync(message);
```

In production, prefer the `MessagePublisher.PublishAsync<T>(...)` helper which sets all of the above from a typed payload.

### 12.2 Sample Wire Format

```
MessageId:                  8b2f9d31-7c4e-4a1b-9e8c-2f5a6c7d8e9f
CorrelationId:              1a2b3c4d-5e6f-7890-abcd-ef0123456789
Subject:                    TransformCompleted
ContentType:                application/json; charset=utf-8

ApplicationProperties:
  X-FileIt-EnqueuedTimeUtc:  2026-05-03T14:32:11.482Z
  X-FileIt-Source:           DataFlow.App
  X-FileIt-EnvelopeVersion:  1

Body:
{
  "envelope": { "version": 1, "schema": "fileit.dataflow.transform.v1" },
  "payload": {
    "sourceFileId":  "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "rowsProcessed": 4827,
    "outputBlobUri": "https://p61856di01fbvrugesmcow.blob.core.windows.net/transformed/2026/05/03/job-8b2f9d31.parquet"
  }
}
```

### 12.3 Dead Lettered Message

A message that failed three times due to a downstream timeout, then landed in DLQ:

```
MessageId:                   (new GUID, post-replay if any; otherwise original)
CorrelationId:               1a2b3c4d-5e6f-7890-abcd-ef0123456789  (preserved)
Subject:                     TransformCompleted

ApplicationProperties:
  X-FileIt-EnqueuedTimeUtc:   2026-05-03T14:32:11.482Z   (preserved from original publish)
  X-FileIt-Source:            DataFlow.App
  X-FileIt-EnvelopeVersion:   1
  X-FileIt-AttemptCount:      3
  X-FileIt-DeadLetterReason:  unhandled-exception

DeadLetterReason:             unhandled-exception
DeadLetterErrorDescription:   System.TimeoutException: ComplexApi did not respond within 30s.
```

The DLQ reader computes age at failure as `now - 2026-05-03T14:32:11.482Z`, not as `now - <native EnqueuedTimeUtc of the DLQ copy>`. This is the whole reason `X-FileIt-EnqueuedTimeUtc` exists; without it, time spent in retry loops and in DLQ would be invisible to the SLO report.

## 13. Open Questions and Future Work

* **Schema registry.** Today `envelope.schema` is a string convention. A future change could publish JSON Schema documents to a registry and have producers and consumers validate against them. Until then, DTO classes in `<Module>.Domain/Messages/` are the source of truth.
* **Compression / Claim Check.** Bodies above 256 KB are rare today, but Service Bus standard tier max is 256 KB. If FinMod ever routes raw GL extracts through Service Bus, we will need the Claim Check pattern (body in blob, message holds the URI). Application property convention reserved: `X-FileIt-ClaimCheckUri`.
* **Field level encryption.** GLAccount data passes through DataFlow today in plaintext. If we ship the SHIELD obfuscation pipeline through Service Bus, we will need an `X-FileIt-PayloadEncryption` property (values: `none`, `field-hmac-sha256`, etc.) and a documented field level cipher convention.
* **Sessions.** No FileIt entity uses sessions today. If we add ordered processing for per customer streams, the conventions for `SessionId` and `ReplyToSessionId` need to be added here. Likely value: `CorrelationId` of the originating logical operation.
* **OpenTelemetry trace propagation.** SDK already sets `Diagnostic-Id` and `traceparent` on the message. Need to confirm interaction with the OpenTelemetry.Api 1.15.1 vulnerability flagged earlier, once that bump happens.

## 14. Glossary

* **Envelope.** The message metadata wrapper that carries an operation across the system. Includes Service Bus native properties and the `X-FileIt-*` application properties, plus the `envelope` block at the JSON root.
* **Subject.** A short PascalCase label on the Service Bus message that names the event (or, rarely, the command).
* **CorrelationId.** A UUID that identifies a logical operation across multiple physical messages.
* **MessageId.** A UUID that identifies one physical message. Regenerated on every retry.
* **Idempotency.** The property that processing the same logical operation more than once produces the same observable side effects as processing it exactly once.
* **DLQ (Dead Letter Queue).** A sub queue of every Service Bus entity that holds messages the consumer rejected, that exceeded `MaxDeliveryCount`, or that the broker itself rejected (TTL, size, etc.).
* **At least once.** A delivery guarantee where the broker may deliver the same logical message more than once. Requires consumer side idempotency.
* **Claim Check pattern.** A messaging pattern where the message body is replaced by a URI pointing at a large payload in blob storage. Consumers fetch the body separately.
* **Schema string.** The value of `envelope.schema`, e.g. `fileit.dataflow.transform.v1`. Canonical identifier for body shape.

## 15. Change Log

| Version | Date | Author | Notes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-05-03 | Proximus | Initial spec, issue #14. |
