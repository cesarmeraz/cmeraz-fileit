using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Queries;

public interface IExportDocumentsQuery
{
    Task<DocumentExportResponse> ExecuteAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);
}

public class ExportDocumentsQuery : IExportDocumentsQuery
{
    private readonly IComplexDocumentRepo _repo;
    private readonly ILogger<ExportDocumentsQuery> _logger;

    public ExportDocumentsQuery(
        IComplexDocumentRepo repo,
        ILogger<ExportDocumentsQuery> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<DocumentExportResponse> ExecuteAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(ComplexEvents.DocumentExportRequested,
            "Document export requested (includeDeleted={IncludeDeleted})", includeDeleted);

        var entities = await _repo.ExportAsync(includeDeleted, cancellationToken)
            .ConfigureAwait(false);

        return new DocumentExportResponse
        {
            Documents = entities.Select(GetDocumentQuery.ToResponse).ToArray(),
            ExportedAtUtc = DateTime.UtcNow,
            Count = entities.Count,
        };
    }
}
