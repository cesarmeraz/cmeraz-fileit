namespace FileIt.Domain.Interfaces;

/// <summary>
/// Idempotency cache contract. Tracks request hashes and cached responses
/// keyed by Idempotency-Key header. Implemented in
/// FileIt.Infrastructure.Data.ComplexIdempotencyRepo.
/// </summary>
public interface IComplexIdempotencyRepo
{
    Task<IdempotencyHit?> TryGetAsync(string key, CancellationToken cancellationToken = default);

    Task SaveAsync(
        string key,
        string requestHash,
        int responseStatusCode,
        string? responseBody,
        string? responseLocation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// What a client gets back when their Idempotency-Key matches a prior request.
/// </summary>
public sealed record IdempotencyHit(
    string Key,
    string RequestHash,
    int ResponseStatusCode,
    string? ResponseBody,
    string? ResponseLocation,
    DateTime CreatedUtc);
