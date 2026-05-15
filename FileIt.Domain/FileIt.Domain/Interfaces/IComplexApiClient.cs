namespace FileIt.Domain.Interfaces;

/// <summary>
/// HTTP client for the Complex module's simulated document API. Lives in
/// Domain so Services and other modules can depend on the abstraction
/// without referencing Complex directly. Implemented in
/// FileIt.Infrastructure.HttpClients.ComplexApiClient.
/// </summary>
public interface IComplexApiClient
{
    Task<ComplexCreateResult> CreateDocumentAsync(
        string name,
        string contentType,
        string content,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<ComplexDocumentDto?> GetDocumentAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight DTO so the contract doesn't leak Domain entities through
/// the wire boundary. Mirrors the JSON the Complex API actually returns.
/// </summary>
public sealed record ComplexDocumentDto(
    Guid Id,
    string Name,
    string ContentType,
    long SizeBytes,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public sealed record ComplexCreateResult(
    Guid Id,
    string Location,
    bool WasIdempotentReplay);
