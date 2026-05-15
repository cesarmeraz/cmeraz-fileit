# CommonLog vs Application Insights

Cesar explicitly called out in [#41](https://github.com/Pr0x1mo/cmeraz-fileit/issues/41) that we should "discover how this set of logs differs in purpose, presentation and audience from app insights logs (#40)." This document answers that question.

Both CommonLog (our SQL Server table) and Application Insights (Azure's managed telemetry platform) receive the same log events from the same Serilog pipeline. They are NOT redundant - they serve different audiences and enable different workflows.

## Quick Comparison

|                          | CommonLog (SQL)                              | Application Insights                                |
|--------------------------|----------------------------------------------|-----------------------------------------------------|
| **Storage**              | SQL Server table, owned by FileIt            | Azure-managed, logs and metrics                     |
| **Write path**           | Custom DatabaseSink (Serilog)                | ApplicationInsights sink (Serilog, RELEASE builds)  |
| **Query language**       | T-SQL                                        | Kusto Query Language (KQL)                          |
| **Retention**            | Unlimited (or policy we define)              | 30-90 days default, configurable                    |
| **Cost**                 | Included in SQL MI / cheap                   | Pay per GB ingested (~$2.30/GB)                     |
| **Primary audience**     | Developers, QA, business UAT                 | Operators, platform engineers, incident response   |
| **Presentation**         | Custom UI (#17) + direct SQL                 | Azure Portal dashboards, workbooks, alerts          |
| **Coupling**             | Tightly coupled to our schema/columns        | Loosely coupled, structured by Serilog properties   |
| **Best for**             | Flow-specific narratives, business context   | System-wide telemetry, alerting, tracing            |

## Purpose Differences

### CommonLog is the flow-of-work story

CommonLog is structured around the business domain. Every row has:

- **CorrelationId** - business flow identifier
- **InvocationId** - single function execution
- **Application** - which host produced the log
- **EventName** - semantic event type
- **SourceContext** - which class was logging

Queries naturally follow the domain: "show me the complete timeline for flow X", "which files failed to process today", "how long did this transform take".

### App Insights is the infrastructure health story

App Insights is structured around system operations. It captures:

- HTTP requests, dependencies, exceptions
- Performance counters, CPU, memory, throughput
- Service map, end-to-end traces across Azure components
- Automatic correlation across function chains via W3C TraceContext
- Live metrics stream

Queries are shaped by operational concerns: "which API calls failed in the last hour", "what is the p95 latency across the pipeline", "alert me if error rate exceeds 2 percent".

## Presentation Differences

### CommonLog

Consumed by:

- The FileIt UI (#17) for business and QA users
- Developers running SQL queries directly for investigation
- Robot Framework test runs (#42) that assert expected events fired
- Automated reports ("daily flow volume by host")

The UI presents CommonLog as a narrative: Recent Runs list, click into a Run for the Timeline, filter by Application or Level. Business users see human-readable EventNames and Messages. They never see KQL.

### App Insights

Consumed by:

- Azure Portal blade with pre-built dashboards
- Custom Workbooks
- KQL saved queries in Log Analytics
- Alerts that fire on thresholds
- Grafana or Power BI via Log Analytics connectors

The audience is technical. Presentations focus on volume, velocity, error rates, service dependencies, distributed traces. Business users rarely touch this directly.

## Audience Differences

| Audience               | Primary tool               | Why                                                                             |
|------------------------|----------------------------|---------------------------------------------------------------------------------|
| Business stakeholders  | FileIt UI (CommonLog)      | They care about "did my file process correctly", not CPU or request count       |
| QA testers             | FileIt UI + SQL (CommonLog)| They assert specific events fired in specific order                             |
| Developers (debugging) | Both                       | CommonLog for domain flow, App Insights for infra dependencies and perf         |
| SREs, platform eng     | App Insights               | They care about infra health, alerting, throughput, SLO tracking                |
| Incident response      | App Insights               | Live metrics stream, alerts, distributed traces across all Azure components     |
| Audit / compliance     | CommonLog                  | Immutable, long-retention, queryable by business flow, owned by us              |

## Overlap and Redundancy

The same log event goes to both sinks. Redundancy is by design:

- If Azure is down, CommonLog keeps recording (and vice versa)
- If we need to reason about a specific flow, CommonLog has the business context
- If we need to reason about a specific Azure dependency, App Insights has the traces
- Retention is separate: App Insights expires, CommonLog persists

## When to use which

**Use CommonLog when:**

- You know the CorrelationId or InvocationId
- You need unlimited retention or audit trails
- You are building business-user UI
- You need to join with other SQL tables (e.g. link a flow to a request log)
- You are writing automated test assertions
- You are running ad-hoc SQL for a developer investigation

**Use App Insights when:**

- You need to alert on error rate spikes or latency
- You are tracing a request across multiple Azure services
- You need a service map visualization
- You are looking at throughput, CPU, memory, or other infra metrics
- You need the real-time Live Metrics Stream during an incident
- You are integrating with Grafana / Power BI / Azure Monitor alerts

## Configuration Today

- CommonLog DatabaseSink runs in all builds, writes every log event to SQL
- App Insights sink is currently gated behind `#if RELEASE` in `CommonLogExtensions.cs`. It writes telemetry via `TelemetryConverter.Traces` when enabled.
- Both sinks share the same Serilog `LoggerConfiguration`, so enrichers and minimum levels apply identically.

## Not yet implemented

The following items belong to #40 rather than #41:

- Define the top 10 KQL queries that ops will need
- Build the Azure Workbook dashboard
- Configure alerts on error rate and latency thresholds
- Hook up Live Metrics Stream
- Document the App Insights resource name, access, and sampling configuration

These can build on this comparison once the infra side has a target App Insights instance.