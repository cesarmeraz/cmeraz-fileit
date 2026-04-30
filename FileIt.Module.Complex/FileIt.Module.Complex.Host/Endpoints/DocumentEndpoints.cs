using System.Net;
using System.Text.Json;
using FileIt.Module.Complex.App;
using FileIt.Module.Complex.App.Behavior;
using FileIt.Module.Complex.App.Commands;
using FileIt.Module.Complex.App.Errors;
using ProblemDetails = FileIt.Module.Complex.App.Errors.ProblemDetails;
using FileIt.Module.Complex.App.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.Host.Endpoints;

/// <summary>
/// All document endpoints. Co-located so the chaos / latency / idempotency
/// pipeline is consistent across them and easy to audit. Each Function
/// method is a thin shell that:
///   1. Runs chaos check (may short-circuit with 503).
///   2. Runs latency injection.
///   3. Delegates to a command/query handler.
///   4. Maps the handler result to an HTTP response (JSON or problem+json).
/// </summary>
public class DocumentEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ComplexConfig _config;
    private readonly IChaosInjector _chaos;
    private readonly ILatencyInjector _latency;
    private readonly IIdempotencyManager _idempotency;
    private readonly ICreateDocumentCommand _createCmd;
    private readonly IDeleteDocumentCommand _deleteCmd;
    private readonly IGetDocumentQuery _getQuery;
    private readonly IListDocumentsQuery _listQuery;
    private readonly IExportDocumentsQuery _exportQuery;
    private readonly ILogger<DocumentEndpoints> _logger;

    public DocumentEndpoints(
        ComplexConfig config,
        IChaosInjector chaos,
        ILatencyInjector latency,
        IIdempotencyManager idempotency,
        ICreateDocumentCommand createCmd,
        IDeleteDocumentCommand deleteCmd,
        IGetDocumentQuery getQuery,
        IListDocumentsQuery listQuery,
        IExportDocumentsQuery exportQuery,
        ILogger<DocumentEndpoints> logger)
    {
        _config = config;
        _chaos = chaos;
        _latency = latency;
        _idempotency = idempotency;
        _createCmd = createCmd;
        _deleteCmd = deleteCmd;
        _getQuery = getQuery;
        _listQuery = listQuery;
        _exportQuery = exportQuery;
        _logger = logger;
    }

    // ---- POST /api/documents -------------------------------------------------

    [Function("Documents_Create")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents")]
            HttpRequest req,
        FunctionContext ctx)
    {
        var ct = ctx.CancellationToken;

        if (TryShortCircuitChaos(req.Path, out var chaosResp))
        {
            return chaosResp!;
        }

        // Read body once. We need the raw text for both deserialization and
        // idempotency hashing.
        string rawBody;
        using (var reader = new StreamReader(req.Body))
        {
            rawBody = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }

        // Idempotency check before anything mutates state.
        string? idempKey = null;
        if (req.Headers.TryGetValue(_config.Idempotency.HeaderName, out var keyValues))
        {
            idempKey = keyValues.ToString();
        }

        var idemp = await _idempotency.CheckAsync(idempKey, rawBody, ct).ConfigureAwait(false);
        switch (idemp.State)
        {
            case IdempotencyState.Invalid:
                return Problem(ProblemDetailsFactory.BadRequest(
                    idemp.RejectReason ?? "Invalid Idempotency-Key.", instance: req.Path));

            case IdempotencyState.Conflict:
                return Problem(new ProblemDetails
                {
                    Type = "https://fileit/problems/idempotency-conflict",
                    Title = "Idempotency Conflict",
                    Status = 422,
                    Detail = idemp.RejectReason,
                    Instance = req.Path,
                });

            case IdempotencyState.Replay:
                return ReplayCached(idemp.Hit!);
        }

        // Deserialize
        CreateDocumentRequest? body;
        try
        {
            body = string.IsNullOrWhiteSpace(rawBody)
                ? null
                : JsonSerializer.Deserialize<CreateDocumentRequest>(rawBody, JsonOpts);
        }
        catch (JsonException ex)
        {
            return Problem(ProblemDetailsFactory.BadRequest(
                $"Invalid JSON body: {ex.Message}", instance: req.Path));
        }
        if (body is null)
        {
            return Problem(ProblemDetailsFactory.BadRequest(
                "Request body is required.", instance: req.Path));
        }

        await _latency.DelayAsync(ct).ConfigureAwait(false);

        var result = await _createCmd.ExecuteAsync(body, ct).ConfigureAwait(false);
        if (!result.Success || result.Document is null)
        {
            return Problem(result.Problem ?? ProblemDetailsFactory.InternalError("Create failed."));
        }

        var location = $"{_config.BaseUrl.TrimEnd('/')}/api/documents/{result.Document.Id}";
        var responseJson = JsonSerializer.Serialize(result.Document, JsonOpts);

        // Save idempotency record after success so retries land here too.
        if (!string.IsNullOrWhiteSpace(idempKey))
        {
            await _idempotency.SaveAsync(
                idempKey, rawBody, 201, responseJson, location, ct).ConfigureAwait(false);
        }

        return new ContentResult
        {
            Content = responseJson,
            ContentType = "application/json",
            StatusCode = 201,
        };
    }

    // ---- GET /api/documents/{id} --------------------------------------------

    [Function("Documents_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/{id}")]
            HttpRequest req,
        string id,
        FunctionContext ctx)
    {
        var ct = ctx.CancellationToken;

        if (TryShortCircuitChaos(req.Path, out var chaosResp))
        {
            return chaosResp!;
        }

        if (!Guid.TryParse(id, out var publicId))
        {
            return Problem(ProblemDetailsFactory.BadRequest(
                $"'{id}' is not a valid document id.", instance: req.Path));
        }

        await _latency.DelayAsync(ct).ConfigureAwait(false);

        var doc = await _getQuery.ExecuteAsync(publicId, ct).ConfigureAwait(false);
        if (doc is null)
        {
            return Problem(ProblemDetailsFactory.NotFound(
                $"Document '{publicId}' not found.", instance: req.Path));
        }
        return new OkObjectResult(doc);
    }

    // ---- GET /api/documents (list) -----------------------------------------

    [Function("Documents_List")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")]
            HttpRequest req,
        FunctionContext ctx)
    {
        var ct = ctx.CancellationToken;

        if (TryShortCircuitChaos(req.Path, out var chaosResp))
        {
            return chaosResp!;
        }

        string? nameFilter = req.Query.TryGetValue("name", out var nameVals) ? nameVals.ToString() : null;
        int skip = int.TryParse(req.Query["skip"], out var s) ? s : 0;
        int take = int.TryParse(req.Query["take"], out var t) ? t : 25;
        bool includeDeleted = bool.TryParse(req.Query["includeDeleted"], out var d) && d;

        await _latency.DelayAsync(ct).ConfigureAwait(false);

        var page = await _listQuery
            .ExecuteAsync(nameFilter, skip, take, includeDeleted, ct)
            .ConfigureAwait(false);
        return new OkObjectResult(page);
    }

    // ---- DELETE /api/documents/{id} ----------------------------------------

    [Function("Documents_Delete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "documents/{id}")]
            HttpRequest req,
        string id,
        FunctionContext ctx)
    {
        var ct = ctx.CancellationToken;

        if (TryShortCircuitChaos(req.Path, out var chaosResp))
        {
            return chaosResp!;
        }

        if (!Guid.TryParse(id, out var publicId))
        {
            return Problem(ProblemDetailsFactory.BadRequest(
                $"'{id}' is not a valid document id.", instance: req.Path));
        }

        await _latency.DelayAsync(ct).ConfigureAwait(false);

        var deleted = await _deleteCmd.ExecuteAsync(publicId, ct).ConfigureAwait(false);
        if (!deleted)
        {
            return Problem(ProblemDetailsFactory.NotFound(
                $"Document '{publicId}' not found.", instance: req.Path));
        }
        return new NoContentResult();
    }

    // ---- GET /api/documents/export -----------------------------------------

    [Function("Documents_Export")]
    public async Task<IActionResult> Export(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/export")]
            HttpRequest req,
        FunctionContext ctx)
    {
        var ct = ctx.CancellationToken;

        if (TryShortCircuitChaos(req.Path, out var chaosResp))
        {
            return chaosResp!;
        }

        bool includeDeleted = bool.TryParse(req.Query["includeDeleted"], out var d) && d;

        await _latency.DelayAsync(ct).ConfigureAwait(false);

        var export = await _exportQuery.ExecuteAsync(includeDeleted, ct).ConfigureAwait(false);
        return new OkObjectResult(export);
    }

    // ---- helpers -----------------------------------------------------------

    private bool TryShortCircuitChaos(string path, out IActionResult? response)
    {
        if (_chaos.ShouldFail(path))
        {
            var problem = ProblemDetailsFactory.ServiceUnavailable(
                "The service is temporarily unavailable. Retry after the indicated delay.",
                _chaos.RetryAfterSeconds,
                instance: path);
            var content = new ContentResult
            {
                Content = JsonSerializer.Serialize(problem, JsonOpts),
                ContentType = ProblemDetailsFactory.ProblemContentType,
                StatusCode = 503,
            };
            // Hosting layer doesn't let us add Retry-After via ContentResult cleanly.
            // The body's `retryAfterSeconds` and the existing Retry-After convention
            // are documented in the OpenAPI spec; clients can rely on either.
            response = content;
            return true;
        }
        response = null;
        return false;
    }

    private static IActionResult Problem(ProblemDetails problem)
    {
        return new ContentResult
        {
            Content = JsonSerializer.Serialize(problem, JsonOpts),
            ContentType = ProblemDetailsFactory.ProblemContentType,
            StatusCode = problem.Status,
        };
    }

    private static IActionResult ReplayCached(FileIt.Domain.Interfaces.IdempotencyHit hit)
    {
        return new ContentResult
        {
            Content = hit.ResponseBody ?? string.Empty,
            ContentType = "application/json",
            StatusCode = hit.ResponseStatusCode,
        };
    }
}
