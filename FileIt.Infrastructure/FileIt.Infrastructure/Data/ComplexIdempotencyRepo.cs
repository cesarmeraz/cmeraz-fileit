using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Data;

/// <summary>
/// EF Core implementation of IComplexIdempotencyRepo. Stored alongside
/// other infra repos.
/// </summary>
public class ComplexIdempotencyRepo : IComplexIdempotencyRepo
{
    private readonly CommonDbContext _db;
    private readonly ILogger<ComplexIdempotencyRepo> _logger;

    public ComplexIdempotencyRepo(CommonDbContext db, ILogger<ComplexIdempotencyRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IdempotencyHit?> TryGetAsync(string key, CancellationToken cancellationToken = default)
    {
        var row = await _db.ComplexIdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) return null;

        return new IdempotencyHit(
            row.Key,
            row.RequestHash,
            row.ResponseStatusCode,
            row.ResponseBody,
            row.ResponseLocation,
            row.CreatedUtc);
    }

    public async Task SaveAsync(
        string key,
        string requestHash,
        int responseStatusCode,
        string? responseBody,
        string? responseLocation,
        CancellationToken cancellationToken = default)
    {
        var row = new ComplexIdempotencyRecord
        {
            Key = key,
            RequestHash = requestHash,
            ResponseStatusCode = responseStatusCode,
            ResponseBody = responseBody,
            ResponseLocation = responseLocation,
            CreatedUtc = DateTime.UtcNow,
        };
        _db.ComplexIdempotencyRecords.Add(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Persisted row mirroring dbo.ComplexIdempotency. Lives in the
/// Infrastructure layer because it's a storage detail, not a domain
/// concept (the domain just sees IdempotencyHit).
/// </summary>
public class ComplexIdempotencyRecord
{
    public long IdempotencyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResponseLocation { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
