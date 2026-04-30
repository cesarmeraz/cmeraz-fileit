using FileIt.Domain.Entities.Complex;

namespace FileIt.Domain.Interfaces;

/// <summary>
/// Persistence contract for ComplexDocument. Implemented in
/// FileIt.Infrastructure.Data.ComplexDocumentRepo. Consumed by the Complex
/// module's command and query handlers.
/// </summary>
public interface IComplexDocumentRepo
{
    Task<ComplexDocument> AddAsync(ComplexDocument document, CancellationToken cancellationToken = default);

    Task<ComplexDocument?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplexDocument>> ListAsync(
        string? nameFilter,
        int skip,
        int take,
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplexDocument>> ExportAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);
}
