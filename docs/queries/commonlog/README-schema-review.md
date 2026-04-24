# CommonLog Table Schema Review

This document reviews the `dbo.CommonLog` table, identifies what each column earns its keep for, flags broken behavior and redundancy, and recommends changes.

This is part of [#41 - Review CommonLog](https://github.com/Pr0x1mo/cmeraz-fileit/issues/41).

## Current Schema
Column                     Type              Nullable  Purpose

Id                         int               NO        Primary key, identity, monotonic insert order
Message                    nvarchar(max)     YES       Rendered log message with parameter values substituted
MessageTemplate            nvarchar(max)     YES       Original template string with {Placeholder} tokens
Level                      nvarchar(100)     YES       Serilog level: Verbose | Debug | Information | Warning | Error | Fatal
Exception                  nvarchar(max)     YES       Full exception text when logged via LogError(ex, ...)
Properties                 nvarchar(max)     YES       JSON blob of all structured properties attached to the log event
Environment                nvarchar(100)     YES       Deployment environment: Development, Staging, Production
MachineName                nvarchar(100)     YES       Host machine that produced the log
Application                nvarchar(100)     YES       Which function host: FileIt.Module.DataFlow.Host, Services.Host, SimpleFlow.Host
ApplicationVersion         nvarchar(100)     YES       Version of the application assembly
InfrastructureVersion      nvarchar(100)     YES       Version of FileIt.Infrastructure
SourceContext              nvarchar(100)     YES       Logger name - typically the class that called _logger.Log
CorrelationId              nvarchar(100)     YES       Cross-invocation flow identifier, spans queue hops and host boundaries
InvocationId               nvarchar(100)     YES       Single function invocation identifier from Azure Functions host
EventId                    int               YES       Numeric event id from Microsoft.Extensions.Logging EventId
EventName                  nvarchar(100)     YES       Human-readable event name from the EventId.Name field (added by #41)
CreatedOn                  datetime2         NO        When the log event was emitted
ModifiedOn                 datetime2         NO        Set equal to CreatedOn (logs are immutable, column exists for IAuditable parity)
## Earning-Their-Keep Assessment

### Load-bearing columns: every query uses these

- **Id** - primary key, drives insert order, used for cursor pagination
- **CreatedOn** - time-based filtering and ordering
- **Level** - filtering errors/warnings vs debug noise
- **Application** - partitioning by host for multi-tenant views
- **CorrelationId** - the whole reason this table exists, links cross-host flows
- **InvocationId** - groups events within one function execution
- **Message** - the human-readable output
- **EventId** / **EventName** - event classification for filtering and UI display
- **SourceContext** - tells you what class produced the log

### Useful but not primary

- **Exception** - only populated for errors but essential when it is
- **MessageTemplate** - lets you filter by pattern rather than rendered value, e.g. find every "Moving blob {BlobName}" regardless of filename. Parquet with pipelines that analyze log patterns.
- **Properties** - forensic use only, nobody queries it directly but handy when reconstructing what a specific log call saw

### Environmental context, low query value

- **Environment**, **MachineName**, **ApplicationVersion**, **InfrastructureVersion** - these change rarely within a deployment. Storing them on every row wastes space but lets you filter historical data by version, which is occasionally useful. A future normalization opportunity.

## Known Issues

### Issue 1: Properties column had broken serialization (fixed in #41)

The original `DatabaseSink.Emit` did `JsonSerializer.Serialize(logEvent.Properties)` directly. Because the values are Serilog-internal types (`ScalarValue`, `StructureValue`, `SequenceValue`), `System.Text.Json` could not render them and emitted `{}` for every value. The column captured WHICH keys existed but never their VALUES.

Example of what the bug produced:
```json
{"ApiId":{},"EventId":{},"SourceContext":{},"CorrelationId":{}}
```

Fix in `DatabaseSink.cs`: a new `SerializeProperties` helper that calls `ToString()` on each `LogEventPropertyValue` and serializes a plain `Dictionary<string,string>`. Output now contains real values, though string values carry their own quote escaping.

Historical rows from before this fix remain broken. They will not be backfilled; only new rows get correct data.

### Issue 2: EventName was missing (fixed in #41)

Before this pass, `EventId` was stored as an int but the name was discarded. Queries showed `EventId=3000` with no way to know that meant `DataFlowWatcher` without reading the code.

Two things were required:
1. Every log call site passing the full `EventId` struct instead of `.Id` (the int). This was done across the codebase in #53.
2. A `GetEventIdName` helper in `DatabaseSink` that reaches into the `StructureValue` and pulls the Name property out.

Added `EventName nvarchar(100) NULL` column with a filtered index on non-null values.

### Issue 3: Exceptions from middleware lack EventId

`ExceptionHandlingMiddleware` logs unhandled exceptions but does not pass an EventId. These rows have NULL for EventId and EventName, which breaks event-based filtering. Suggested fix belongs in #22 (dead letter strategy): define an `InfrastructureEvents.UnhandledException` EventId and use it in the middleware.

### Issue 4: CorrelationId is nullable and often NULL for middleware events

`FunctionStart` and `FunctionEnd` events run before correlation is known, so they have `CorrelationId=NULL`. This is by design, not a bug, but it affects queries: filtering `WHERE CorrelationId = @id` misses the start/end bookends of that flow. Queries like `02-correlation-timeline.sql` should document this limitation. Alternative: have middleware set correlation from either the trigger metadata or a synthetic GUID so every row has a correlation, even the bookends.

### Issue 5: ModifiedOn is redundant

Logs are immutable - `ModifiedOn` is always equal to `CreatedOn`. The column exists because `CommonLog` implements `IAuditable`, but it provides no value for log data. Could be dropped or the interface relaxed for read-only log entities. Low priority.

## Recommendations

### For this iteration (#41)

1.  Add `EventName` column and filtered index
2.  Fix Properties serialization in `DatabaseSink`
3.  Thread `EventId` (full struct) through all log call sites (#53)
4.  Add 8 parameterized query files as a library
5.  Document everything in this file and the App Insights comparison

### For future iterations

1. Add an `UnhandledException` EventId used by `ExceptionHandlingMiddleware` so middleware exceptions get proper event classification (belongs with #22)
2. Have middleware populate `CorrelationId` on `FunctionStart` and `FunctionEnd` so queries do not lose bookend rows
3. Consider promoting common computed columns to persisted columns or views for performance if data volume grows past a few million rows
4. Add a monthly partitioning strategy if the table becomes multi-gigabyte

## Indexes

Current indexes:
- PK clustered on Id
- `IX_CommonLog_EventName` filtered index on non-null EventName (added by #41)

Recommended additional indexes for query performance:
- `IX_CommonLog_CorrelationId` on CorrelationId where not null - powers query 02
- `IX_CommonLog_InvocationId` on InvocationId where not null - powers query 03
- `IX_CommonLog_CreatedOn` on CreatedOn - powers queries 04, 05, 06, 07
- Composite `IX_CommonLog_Application_CreatedOn` for dashboard views that filter by host and time

None of these are blocking at current data volumes (131K rows total, local dev). Consider them when prod CommonLog exceeds a few million rows.

## Open Question: Retention

No retention policy exists. At current ingest rate (many rows per minute during dev, much higher in prod), this table will grow unbounded. Two options to consider:

1. **Time-based truncation** - drop rows older than N days via a scheduled job
2. **Partitioned rolling** - partition by month, drop old partitions

Recommendation: do not solve this now. Define retention when the production volume is known.