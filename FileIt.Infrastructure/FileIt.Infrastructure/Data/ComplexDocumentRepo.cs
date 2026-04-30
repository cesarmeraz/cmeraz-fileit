using FileIt.Domain.Entities.Complex;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Data;

/// <summary>
/// EF Core implementation of IComplexDocumentRepo against the FileIt
/// database. Lives alongside the other repos in
/// FileIt.Infrastructure/Data/ so DI registration in
/// AddInfrastructure() picks it up by convention.
/// </summary>
public class ComplexDocumentRepo : IComplexDocumentRepo
{
    private readonly CommonDbContext _db;
    private readonly ILogger<ComplexDocumentRepo> _logger;

    public ComplexDocumentRepo(CommonDbContext db, ILogger<ComplexDocumentRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ComplexDocument> AddAsync(
        ComplexDocument document,
        CancellationToken cancellationToken = default)
    {
        _db.ComplexDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document;
    }

    public async Task<ComplexDocument?> GetByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ComplexDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.PublicId == publicId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ComplexDocument>> ListAsync(
        string? nameFilter,
        int skip,
        int take,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ComplexDocument> q = _db.ComplexDocuments.AsNoTracking();
        if (!includeDeleted)
        {
            q = q.Where(d => d.DeletedUtc == null);
        }
        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            q = q.Where(d => EF.Functions.Like(d.Name, $"%{nameFilter}%"));
        }
        q = q.OrderByDescending(d => d.ModifiedUtc).Skip(skip).Take(take);
        return await q.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _db.ComplexDocuments
            .FirstOrDefaultAsync(d => d.PublicId == publicId, cancellationToken)
            .ConfigureAwait(false);
        if (doc is null || doc.DeletedUtc.HasValue)
        {
            return false;
        }
        doc.DeletedUtc = DateTime.UtcNow;
        doc.ModifiedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<ComplexDocument>> ExportAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ComplexDocument> q = _db.ComplexDocuments.AsNoTracking();
        if (!includeDeleted)
        {
            q = q.Where(d => d.DeletedUtc == null);
        }
        q = q.OrderBy(d => d.CreatedUtc);
        return await q.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
