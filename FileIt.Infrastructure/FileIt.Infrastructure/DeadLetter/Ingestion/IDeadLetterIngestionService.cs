using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.DeadLetter.Ingestion;

/// <summary>
/// Single seam between Service Bus dead-letter receivers and the FileIt domain.
/// Reader functions construct a <see cref="DeadLetterIngestionEnvelope"/> from their
/// trigger arguments and hand it to <see cref="IngestAsync"/>; everything else
/// (classification, persistence, idempotency, observability) lives behind this contract.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be safe to invoke concurrently. The underlying repo uses
/// short-lived <c>DbContext</c> instances per call, so concurrency is bounded by
/// SQL Server's connection pool, not by the service.
/// </para>
/// <para>
/// Implementations must honor idempotency. The same envelope ingested twice (because
/// a reader function crashed before completing the DLQ message and Service Bus
/// re-delivered) must produce exactly one row in <c>dbo.DeadLetterRecord</c> and
/// must not raise to the caller.
/// </para>
/// </remarks>
public interface IDeadLetterIngestionService
{
    /// <summary>
    /// Classifies the dead-lettered message, persists a <see cref="DeadLetterRecord"/>
    /// for it (idempotent on duplicate delivery), and emits the appropriate audit log
    /// entries to <c>CommonLog</c>.
    /// </summary>
    /// <returns>
    /// The persisted (or pre-existing, in the duplicate case) record. Callers may use
    /// the returned <see cref="DeadLetterRecord.DeadLetterRecordId"/> for downstream
    /// correlation but must not assume that <c>DeadLetterRecordId</c> reflects a row
    /// inserted by this call.
    /// </returns>
    /// <exception cref="ArgumentNullException">When <paramref name="envelope"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// When <paramref name="cancellationToken"/> is signaled. Propagated unchanged so
    /// the calling reader can complete its own teardown.
    /// </exception>
    /// <remarks>
    /// All other exceptions (transient SQL faults, classifier defects) are allowed
    /// to propagate. The reader is expected to abandon the dead-letter message and
    /// let Service Bus redeliver, where a transient fault clears on the next attempt
    /// and a persistent fault eventually exceeds <c>MaxDeliveryCount</c> on the DLQ
    /// itself, which is an operations-visible signal.
    /// </remarks>
    Task<DeadLetterRecord> IngestAsync(
        DeadLetterIngestionEnvelope envelope,
        CancellationToken cancellationToken = default);
}