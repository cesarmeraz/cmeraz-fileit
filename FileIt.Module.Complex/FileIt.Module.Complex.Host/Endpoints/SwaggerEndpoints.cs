using FileIt.Module.Complex.App;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.Host.Endpoints;

/// <summary>
/// Hand-written OpenAPI 3.0.3 spec + Swagger UI. We do not use
/// Swashbuckle's runtime middleware because Azure Functions Isolated
/// doesn't expose the ASP.NET Core middleware pipeline cleanly enough to
/// host /swagger UI without trickery. Hand-writing the spec is clearer
/// for a demo, easier to audit, and trivial to maintain. The schema names
/// match the DTOs in App/.
/// </summary>
public class SwaggerEndpoints
{
    private readonly ComplexConfig _config;
    private readonly ILogger<SwaggerEndpoints> _logger;

    public SwaggerEndpoints(ComplexConfig config, ILogger<SwaggerEndpoints> logger)
    {
        _config = config;
        _logger = logger;
    }

    [Function("Swagger_Spec")]
    public IActionResult Spec(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "docs/swagger.json")]
            HttpRequest req)
    {
        _logger.LogInformation(ComplexEvents.SwaggerSpecServed, "OpenAPI spec served");

        return new ContentResult
        {
            Content = BuildSpec(_config.BaseUrl),
            ContentType = "application/json",
            StatusCode = 200,
        };
    }

    [Function("Swagger_UI")]
    public IActionResult UI(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "docs")]
            HttpRequest req)
    {
        return new ContentResult
        {
            Content = SwaggerUiHtml,
            ContentType = "text/html; charset=utf-8",
            StatusCode = 200,
        };
    }

    private static string BuildSpec(string baseUrl) => $$"""
{
  "openapi": "3.0.3",
  "info": {
    "title": "FileIt Complex API",
    "description": "Simulated third-party document API used in FileIt demos. Implements latency injection, chaos failure injection (synthetic 503 with Retry-After), Idempotency-Key support, and RFC 7807 Problem Details responses.",
    "version": "1.0.0"
  },
  "servers": [
    { "url": "{{baseUrl}}", "description": "this server" }
  ],
  "paths": {
    "/api/documents": {
      "get": {
        "summary": "List documents",
        "operationId": "listDocuments",
        "tags": ["documents"],
        "parameters": [
          { "name": "name", "in": "query", "schema": { "type": "string" }, "description": "Substring filter on document name." },
          { "name": "skip", "in": "query", "schema": { "type": "integer", "default": 0 } },
          { "name": "take", "in": "query", "schema": { "type": "integer", "default": 25 } },
          { "name": "includeDeleted", "in": "query", "schema": { "type": "boolean", "default": false } }
        ],
        "responses": {
          "200": { "description": "Paged list", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/DocumentListResponse" } } } },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      },
      "post": {
        "summary": "Create a document",
        "operationId": "createDocument",
        "tags": ["documents"],
        "parameters": [
          { "name": "Idempotency-Key", "in": "header", "schema": { "type": "string", "maxLength": 128 }, "description": "Optional idempotency key. Repeating the same key with the same body returns the cached response. Reusing with a different body returns 422." }
        ],
        "requestBody": {
          "required": true,
          "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateDocumentRequest" } } }
        },
        "responses": {
          "201": { "description": "Created", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/DocumentResponse" } } } },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "413": { "$ref": "#/components/responses/PayloadTooLarge" },
          "422": { "$ref": "#/components/responses/IdempotencyConflict" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/documents/{id}": {
      "get": {
        "summary": "Get a document by id",
        "operationId": "getDocument",
        "tags": ["documents"],
        "parameters": [
          { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
        ],
        "responses": {
          "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/DocumentResponse" } } } },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "404": { "$ref": "#/components/responses/NotFound" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      },
      "delete": {
        "summary": "Soft-delete a document",
        "operationId": "deleteDocument",
        "tags": ["documents"],
        "parameters": [
          { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
        ],
        "responses": {
          "204": { "description": "Deleted" },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "404": { "$ref": "#/components/responses/NotFound" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/documents/export": {
      "get": {
        "summary": "Export all documents",
        "operationId": "exportDocuments",
        "tags": ["documents"],
        "parameters": [
          { "name": "includeDeleted", "in": "query", "schema": { "type": "boolean", "default": false } }
        ],
        "responses": {
          "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/DocumentExportResponse" } } } },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/health": {
      "get": {
        "summary": "Liveness probe",
        "operationId": "health",
        "tags": ["health"],
        "responses": {
          "200": { "description": "OK" }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "CreateDocumentRequest": {
        "type": "object",
        "required": ["name", "content"],
        "properties": {
          "name": { "type": "string", "maxLength": 260 },
          "contentType": { "type": "string", "default": "text/plain" },
          "content": { "type": "string" }
        }
      },
      "DocumentResponse": {
        "type": "object",
        "properties": {
          "id": { "type": "string", "format": "uuid" },
          "name": { "type": "string" },
          "contentType": { "type": "string" },
          "sizeBytes": { "type": "integer", "format": "int64" },
          "content": { "type": "string", "nullable": true },
          "createdUtc": { "type": "string", "format": "date-time" },
          "modifiedUtc": { "type": "string", "format": "date-time" }
        }
      },
      "DocumentListResponse": {
        "type": "object",
        "properties": {
          "items": { "type": "array", "items": { "$ref": "#/components/schemas/DocumentResponse" } },
          "skip": { "type": "integer" },
          "take": { "type": "integer" },
          "includeDeleted": { "type": "boolean" },
          "nameFilter": { "type": "string", "nullable": true }
        }
      },
      "DocumentExportResponse": {
        "type": "object",
        "properties": {
          "documents": { "type": "array", "items": { "$ref": "#/components/schemas/DocumentResponse" } },
          "exportedAtUtc": { "type": "string", "format": "date-time" },
          "count": { "type": "integer" }
        }
      },
      "ProblemDetails": {
        "type": "object",
        "properties": {
          "type": { "type": "string", "format": "uri" },
          "title": { "type": "string" },
          "status": { "type": "integer" },
          "detail": { "type": "string", "nullable": true },
          "instance": { "type": "string", "nullable": true },
          "correlationId": { "type": "string", "nullable": true },
          "traceId": { "type": "string", "nullable": true },
          "errors": { "type": "object", "additionalProperties": { "type": "array", "items": { "type": "string" } } }
        }
      }
    },
    "responses": {
      "BadRequest": {
        "description": "Bad Request",
        "content": { "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } } }
      },
      "NotFound": {
        "description": "Not Found",
        "content": { "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } } }
      },
      "PayloadTooLarge": {
        "description": "Payload Too Large",
        "content": { "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } } }
      },
      "IdempotencyConflict": {
        "description": "Idempotency Conflict",
        "content": { "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } } }
      },
      "ServiceUnavailable": {
        "description": "Service Unavailable (synthetic chaos failure)",
        "content": { "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } } }
      }
    }
  }
}
""";

    /// <summary>
    /// Self-contained Swagger UI page. Loads the UI assets from the
    /// official CDN so we don't bundle a copy of swagger-ui-dist.
    /// </summary>
    private const string SwaggerUiHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>FileIt Complex API</title>
  <link rel="stylesheet" type="text/css" href="https://unpkg.com/swagger-ui-dist@5.17.14/swagger-ui.css" />
  <style>body { margin: 0; }</style>
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="https://unpkg.com/swagger-ui-dist@5.17.14/swagger-ui-bundle.js"></script>
  <script>
    window.onload = () => {
      window.ui = SwaggerUIBundle({
        url: 'swagger.json',
        dom_id: '#swagger-ui',
        deepLinking: true,
        defaultModelsExpandDepth: 0,
      });
    };
  </script>
</body>
</html>
""";
}
