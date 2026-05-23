namespace FileIt.Infrastructure.DeadLetter.Ingestion;

public static class DeadLetterIngestionServiceExtensions
{
    /// <summary>
    /// Ingests a dead-letter envelope while intentionally discarding the persisted
    /// domain record result. Useful for host adapters that should not depend on
    /// domain entity types.
    /// </summary>
    public static async Task IngestWithoutResultAsync(
        this IDeadLetterIngestionService ingestion,
        DeadLetterIngestionEnvelope envelope,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(ingestion);
        await ingestion.IngestAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
