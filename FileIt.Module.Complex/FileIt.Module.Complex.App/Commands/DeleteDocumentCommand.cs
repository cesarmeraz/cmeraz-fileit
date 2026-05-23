using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Commands;

public interface IDeleteDocumentCommand
{
    /// <summary>True if a document was soft-deleted; false if not found.</summary>
    Task<bool> ExecuteAsync(Guid publicId, CancellationToken cancellationToken = default);
}

public class DeleteDocumentCommand : IDeleteDocumentCommand
{
    private readonly IComplexDocumentRepo _repo;
    private readonly ILogger<DeleteDocumentCommand> _logger;

    public DeleteDocumentCommand(
        IComplexDocumentRepo repo,
        ILogger<DeleteDocumentCommand> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var deleted = await _repo.SoftDeleteAsync(publicId, cancellationToken).ConfigureAwait(false);
        if (deleted)
        {
            _logger.LogInformation(ComplexEvents.DocumentSoftDeleted,
                "Document soft-deleted: {PublicId}", publicId);
        }
        else
        {
            _logger.LogInformation(ComplexEvents.DocumentNotFound,
                "Document not found for delete: {PublicId}", publicId);
        }
        return deleted;
    }
}
