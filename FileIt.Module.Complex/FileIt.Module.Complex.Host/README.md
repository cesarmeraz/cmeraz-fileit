# FileIt.Module.Complex

A simulated third-party document API used in FileIt demos. Implements a
realistic HTTP API surface with persistent storage, controlled
unreliability, and idempotent semantics so the rest of the FileIt
pipeline can be exercised against something that behaves like a real
external service instead of the previous "Imaginary" stub.

Resolves issue #10.

## What it does

Five HTTP endpoints under `/api/`:

  POST   /api/documents            create
  GET    /api/documents            list (with ?name= filter, paging)
  GET    /api/documents/{id}       fetch one
  DELETE /api/documents/{id}       soft-delete
  GET    /api/documents/export     bulk export

Plus:

  GET    /api/health               liveness probe
  GET    /api/docs                 Swagger UI
  GET    /api/docs/swagger.json    OpenAPI 3.0.3 spec

## Behaviors

These are configured per environment via the `Feature` section in
`appsettings.json`. Defaults are tuned for "demo realism".

1. Latency injection: uniform random delay between MinMs and MaxMs on
   every response. Default 50 to 300 ms. Disable in LocalDev.

2. Chaos failure injection: with probability `Failure503Rate` (default
   0.05) the response is replaced with `503 Service Unavailable` plus an
   RFC 7807 `application/problem+json` body. Health and Swagger paths
   are exempt so probes and demo navigation never flake.

3. Idempotency-Key header: POST requests can include the standard
   `Idempotency-Key` header. Repeating the same key with the same body
   replays the cached response. Repeating with a different body returns
   `422` with a clear conflict message. Keys longer than the configured
   max are rejected with `400`.

4. RFC 7807 problem details: every non-success response uses
   `application/problem+json` with `type / title / status / detail /
   instance` plus extension fields `correlationId`, `traceId`, and
   `errors` for field-level validation.

## Persistence

Two new tables in the `FileIt` database:

  dbo.ComplexDocument        document state
  dbo.ComplexIdempotency     idempotency-key cache

Schema lives in `FileIt.Database/Tables/`. Deploy with
`scripts/deploy-database.ps1` from the repo root. The DACPAC pipeline
treats these like any other table, so a fresh deploy includes them
automatically.

## Local development

  cd FileIt.Module.Complex/FileIt.Module.Complex.Host
  dotnet run

Then open `http://localhost:7064/api/docs` in a browser. The Swagger UI
is wired against the live host, so requests fired from the UI hit real
endpoints.

For end-to-end with the rest of FileIt, run from the repo root:

  cd FileIt.AppHost
  dotnet run

Aspire orchestrates Complex alongside SimpleFlow, DataFlow, and
Services, and Services' `IComplexApiClient` is wired to the Complex
host's URL automatically.

## Architecture

  Layers:
    Host          HTTP endpoints, DI wiring, host config
    App           commands, queries, behaviors, DTOs, errors
    Test          unit tests for behaviors and command logic
    Integration   smoke tests against a running host

Behavior pipeline on every endpoint:

  request -> chaos check -> latency injection -> handler -> response mapper

The chaos check runs first so synthetic 503s exit before any work
starts. Latency runs after so legitimate retries aren't double-penalised.

## EventId catalog

Block 4000 to 4099 reserved for Complex per the project convention
(Services=1000, SimpleFlow=2000, DataFlow=3000, Complex=4000). Full
catalog in `App/ComplexEvents.cs`.

## Why hand-written OpenAPI

Swashbuckle's runtime middleware wants the full ASP.NET Core pipeline,
which Azure Functions Isolated does not expose cleanly. Hand-writing the
spec is small (a few hundred lines), gives us perfect control of the
schema names and examples, and avoids an entire dependency on
swagger-runtime that would have to be configured per host. The Swagger
UI page itself loads the official `swagger-ui-dist` CDN bundle, so we
get the full UX without bundling assets.
