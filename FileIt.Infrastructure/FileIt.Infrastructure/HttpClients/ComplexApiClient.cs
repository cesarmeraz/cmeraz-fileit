using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.HttpClients;

/// <summary>
/// Real HTTP implementation of IComplexApiClient. Talks to the Complex
/// module's HTTP API. Should be registered with HttpClientFactory and
/// configured with BaseAddress pointing to FileIt.Module.Complex.Host.
/// </summary>
public class ComplexApiClient : IComplexApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _http;
    private readonly ILogger<ComplexApiClient> _logger;

    public ComplexApiClient(HttpClient http, ILogger<ComplexApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ComplexCreateResult> CreateDocumentAsync(
        string name,
        string contentType,
        string content,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var body = new { name, contentType, content };
        using var msg = new HttpRequestMessage(HttpMethod.Post, "api/documents")
        {
            Content = JsonContent.Create(body, options: JsonOpts),
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            msg.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var resp = await _http.SendAsync(msg, cancellationToken).ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            // Synthetic chaos failure. Caller (the Service Bus subscriber)
            // will rethrow so the broker retries.
            var retryAfter = resp.Headers.RetryAfter?.Delta?.TotalSeconds ?? 2;
            throw new ComplexApiUnavailableException(
                $"Complex API returned 503; retry after {retryAfter}s.");
        }

        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content
            .ReadFromJsonAsync<DocumentResponseWire>(JsonOpts, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response body from Complex API.");

        var location = resp.Headers.Location?.ToString() ?? $"api/documents/{dto.Id}";

        // Whether this was a fresh create or an idempotent replay, body shape
        // is identical. We can't currently distinguish from response headers
        // because the simple Complex implementation doesn't expose a custom
        // "X-Idempotent-Replay" header. Return false defensively; this field
        // is informational only.
        return new ComplexCreateResult(dto.Id, location, WasIdempotentReplay: false);
    }

    public async Task<ComplexDocumentDto?> GetDocumentAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        using var resp = await _http
            .GetAsync($"api/documents/{publicId}", cancellationToken)
            .ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content
            .ReadFromJsonAsync<DocumentResponseWire>(JsonOpts, cancellationToken)
            .ConfigureAwait(false);
        if (dto is null) return null;

        return new ComplexDocumentDto(
            dto.Id,
            dto.Name,
            dto.ContentType,
            dto.SizeBytes,
            dto.CreatedUtc,
            dto.ModifiedUtc);
    }

    private sealed class DocumentResponseWire
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }
}

public class ComplexApiUnavailableException : Exception
{
    public ComplexApiUnavailableException(string message) : base(message) { }
}
