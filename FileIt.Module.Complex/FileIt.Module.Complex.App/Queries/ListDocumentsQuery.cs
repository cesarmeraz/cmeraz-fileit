using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Queries;

public interface IListDocumentsQuery
{
    Task<DocumentListResponse> ExecuteAsync(
        string? nameFilter,
        int skip,
        int take,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
}

public class ListDocumentsQuery : IListDocumentsQuery
{
    private readonly ComplexConfig _config;
    private readonly IComplexDocumentRepo _repo;
    private readonly ILogger<ListDocumentsQuery> _logger;

    public ListDocumentsQuery(
        ComplexConfig config,
        IComplexDocumentRepo repo,
        ILogger<ListDocumentsQuery> logger)
    {
        _config = config;
        _repo = repo;
        _logger = logger;
    }

    public async Task<DocumentListResponse> ExecuteAsync(
        string? nameFilter,
        int skip,
        int take,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take <= 0 ? 25 : take, 1, _config.MaxPageSize);

        var entities = await _repo.ListAsync(
                nameFilter,
                safeSkip,
                safeTake,
                includeDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(ComplexEvents.DocumentListed,
            "Listed documents: filter={NameFilter}, skip={Skip}, take={Take}, includeDeleted={IncludeDeleted}, count={Count}",
            nameFilter ?? "<none>", safeSkip, safeTake, includeDeleted, entities.Count);

        return new DocumentListResponse
        {
            Items = entities.Select(GetDocumentQuery.ToResponse).ToArray(),
            Skip = safeSkip,
            Take = safeTake,
            IncludeDeleted = includeDeleted,
            NameFilter = nameFilter,
        };
    }
}
