using FileIt.Domain.Entities.Complex;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Queries;

public interface IGetDocumentQuery
{
    Task<DocumentResponse?> ExecuteAsync(Guid publicId, CancellationToken cancellationToken = default);
}

public class GetDocumentQuery : IGetDocumentQuery
{
    private readonly IComplexDocumentRepo _repo;
    private readonly ILogger<GetDocumentQuery> _logger;

    public GetDocumentQuery(
        IComplexDocumentRepo repo,
        ILogger<GetDocumentQuery> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<DocumentResponse?> ExecuteAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByPublicIdAsync(publicId, cancellationToken).ConfigureAwait(false);

        if (entity is null || entity.IsDeleted)
        {
            _logger.LogInformation(ComplexEvents.DocumentNotFound,
                "Document not found: {PublicId}", publicId);
            return null;
        }

        _logger.LogInformation(ComplexEvents.DocumentRetrieved,
            "Document retrieved: {PublicId} ({Name})", entity.PublicId, entity.Name);

        return ToResponse(entity);
    }

    internal static DocumentResponse ToResponse(ComplexDocument d) => new()
    {
        Id = d.PublicId,
        Name = d.Name,
        ContentType = d.ContentType,
        SizeBytes = d.SizeBytes,
        Content = d.Content,
        CreatedUtc = d.CreatedUtc,
        ModifiedUtc = d.ModifiedUtc,
    };
}
