using System.Security.Cryptography;
using System.Text;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Behavior;

/// <summary>
/// Result of attempting to use an Idempotency-Key on a POST.
/// </summary>
public sealed record IdempotencyOutcome(
    IdempotencyState State,
    IdempotencyHit? Hit,
    string? RejectReason);

public enum IdempotencyState
{
    /// <summary>Key absent or feature disabled. Proceed normally.</summary>
    Skip,
    /// <summary>Key present, never seen before. Proceed and save the result after.</summary>
    Proceed,
    /// <summary>Key seen with the same payload hash. Replay the cached response.</summary>
    Replay,
    /// <summary>Key seen but with a different payload. Reject 422.</summary>
    Conflict,
    /// <summary>Key malformed or too long. Reject 400.</summary>
    Invalid,
}

public interface IIdempotencyManager
{
    Task<IdempotencyOutcome> CheckAsync(
        string? idempotencyKey,
        string requestBody,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string idempotencyKey,
        string requestBody,
        int responseStatusCode,
        string? responseBody,
        string? responseLocation,
        CancellationToken cancellationToken = default);

    string ComputeRequestHash(string requestBody);
}

public class IdempotencyManager : IIdempotencyManager
{
    private readonly ComplexConfig _config;
    private readonly IComplexIdempotencyRepo _repo;
    private readonly ILogger<IdempotencyManager> _logger;

    public IdempotencyManager(
        ComplexConfig config,
        IComplexIdempotencyRepo repo,
        ILogger<IdempotencyManager> logger)
    {
        _config = config;
        _repo = repo;
        _logger = logger;
    }

    public string ComputeRequestHash(string requestBody)
    {
        // SHA-256 hex. Same body -> same hash. Different body with same key
        // -> different hash -> conflict.
        var bytes = Encoding.UTF8.GetBytes(requestBody ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<IdempotencyOutcome> CheckAsync(
        string? idempotencyKey,
        string requestBody,
        CancellationToken cancellationToken = default)
    {
        if (!_config.Idempotency.Enabled || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdempotencyOutcome(IdempotencyState.Skip, null, null);
        }

        if (idempotencyKey.Length > _config.Idempotency.MaxKeyLength)
        {
            _logger.LogWarning(ComplexEvents.IdempotencyKeyRejected,
                "Idempotency-Key too long: {Length} > {Max}",
                idempotencyKey.Length, _config.Idempotency.MaxKeyLength);
            return new IdempotencyOutcome(
                IdempotencyState.Invalid,
                null,
                $"Idempotency-Key length {idempotencyKey.Length} exceeds maximum {_config.Idempotency.MaxKeyLength}.");
        }

        var hit = await _repo.TryGetAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
        if (hit == null)
        {
            return new IdempotencyOutcome(IdempotencyState.Proceed, null, null);
        }

        var requestHash = ComputeRequestHash(requestBody);
        if (!string.Equals(hit.RequestHash, requestHash, StringComparison.Ordinal))
        {
            _logger.LogWarning(ComplexEvents.IdempotencyKeyConflict,
                "Idempotency-Key {Key} reused with a different request body",
                idempotencyKey);
            return new IdempotencyOutcome(
                IdempotencyState.Conflict,
                hit,
                "Idempotency-Key has already been used with a different request payload.");
        }

        _logger.LogInformation(ComplexEvents.IdempotentReplay,
            "Replaying cached response for Idempotency-Key {Key} (status {Status})",
            idempotencyKey, hit.ResponseStatusCode);
        return new IdempotencyOutcome(IdempotencyState.Replay, hit, null);
    }

    public Task SaveAsync(
        string idempotencyKey,
        string requestBody,
        int responseStatusCode,
        string? responseBody,
        string? responseLocation,
        CancellationToken cancellationToken = default)
    {
        if (!_config.Idempotency.Enabled || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Task.CompletedTask;
        }

        var hash = ComputeRequestHash(requestBody);
        return _repo.SaveAsync(
            idempotencyKey,
            hash,
            responseStatusCode,
            responseBody,
            responseLocation,
            cancellationToken);
    }
}
