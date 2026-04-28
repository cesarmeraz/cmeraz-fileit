using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.DeadLetter.Replay;

/// <summary>
/// Replays dead-lettered messages back to their source channel.
/// </summary>
/// <remarks>
/// <para>
/// Replay is opt-in: only records in <see cref="DeadLetterRecordStatus.PendingReplay"/>
/// are eligible. Operators promote a record to <c>PendingReplay</c> via the FileIt UI
/// or a SQL update; the replay service then picks it up on its next run. Automatic
/// promotion (e.g. "every Transient older than N minutes") is deliberately out of
/// scope at this layer. See docs/dead-letter-strategy.md Section 3 for the rationale.
/// </para>
/// <para>
/// Implementations must be safe to invoke concurrently. Two replay invocations racing
/// on the same record is mitigated by the lifecycle update (<see cref="DeadLetterRecordStatus.Replayed"/>)
/// happening inside the service: the second invocation reads a status that is no
/// longer <c>PendingReplay</c> and skips the record cleanly.
/// </para>
/// <para>
/// Cancellation is honored at every async boundary. A canceled replay leaves
/// in-flight records in <c>PendingReplay</c> so a subsequent run picks them up; no
/// silent partial state.
/// </para>
/// </remarks>
public interface IDeadLetterReplayService
{
    /// <summary>
    /// Replay a single record by id. Used by the HTTP trigger for operator-driven
    /// per-record replay.
    /// </summary>
    /// <param name="deadLetterRecordId">Primary key of the record to replay.</param>
    /// <param name="initiatedBy">
    /// Identity of the actor that initiated the replay. Persisted into
    /// <see cref="DeadLetterRecord.StatusUpdatedBy"/> for audit.
    /// </param>
    /// <returns>
    /// A <see cref="DeadLetterReplayOutcome"/> describing the result. Never throws on
    /// expected failure modes (record not found, wrong status, send failure); those
    /// are encoded in the outcome. Throws only on programmer errors and infrastructure
    /// failures unrelated to the replay itself (e.g. database connection lost).
    /// </returns>
    Task<DeadLetterReplayOutcome> ReplayAsync(
        long deadLetterRecordId,
        string initiatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replay up to <paramref name="maxRecords"/> records currently in
    /// <see cref="DeadLetterRecordStatus.PendingReplay"/>. Used by the timer trigger
    /// for batch replay. Records are processed in <c>StatusUpdatedUtc</c> order
    /// (oldest first) so a backlog drains in FIFO fashion.
    /// </summary>
    /// <param name="maxRecords">
    /// Hard ceiling on records processed in this batch. The service stops after this
    /// many even if more eligible records exist; the next run picks up the rest.
    /// Bounds the worst-case duration of a single timer tick.
    /// </param>
    /// <param name="initiatedBy">
    /// Identity of the actor that initiated the batch. For timer-driven runs this is
    /// typically the function name (e.g. <c>"DeadLetterReplayTimer"</c>).
    /// </param>
    /// <returns>
    /// One outcome per record processed, in the order they were processed. Empty if
    /// no eligible records existed at the time of the call.
    /// </returns>
    Task<IReadOnlyList<DeadLetterReplayOutcome>> ReplayBatchAsync(
        int maxRecords,
        string initiatedBy,
        CancellationToken cancellationToken = default);
}