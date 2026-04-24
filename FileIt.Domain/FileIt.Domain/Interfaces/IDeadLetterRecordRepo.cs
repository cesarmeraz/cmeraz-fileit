using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Domain.Interfaces;

/// <summary>
/// Repository for <see cref="DeadLetterRecord"/>. Used by DLQ reader functions to
/// persist new records, by the replay function to pull and update candidates, and
/// by operator query paths.
/// </summary>
/// <remarks>
/// Deliberately does NOT inherit from <c>IRepository&lt;T&gt;</c>. The shared
/// contract assumes <c>int</c> primary keys and <c>IAuditable</c> timestamp semantics
/// that do not fit this entity (see <see cref="DeadLetterRecord"/> for the full
/// rationale). Exposing only purpose-built methods also prevents callers from
/// accidentally invoking generic CRUD paths that would bypass the
/// <see cref="InsertAsync"/> idempotency contract or the lifecycle invariants enforced
/// by <see cref="UpdateLifecycleAsync"/>.
/// </remarks>
public interface IDeadLetterRecordRepo
{
    /// <summary>
    /// Insert a new dead-letter record. Intended to be called by DLQ reader functions
    /// after classification. Returns the persisted entity with its server-assigned
    /// <see cref="DeadLetterRecord.DeadLetterRecordId"/>.
    /// </summary>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">
    /// Thrown when the unique index
    /// <c>IX_DeadLetterRecord_MessageId_Source_DeadLetteredTime</c> rejects the insert
    /// because an equivalent row already exists. Callers should treat this as an
    /// idempotency signal: the message was already recorded by a previous delivery
    /// of this dead-letter receive, and the current delivery can be completed
    /// without further action.
    /// </exception>
    Task<DeadLetterRecord> InsertAsync(
        DeadLetterRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a record already exists for the given identity tuple. Used by
    /// readers as a friendly pre-check before <see cref="InsertAsync"/>; the database
    /// unique index remains the final authority on uniqueness.
    /// </summary>
    Task<bool> ExistsAsync(
        string messageId,
        string sourceEntityName,
        DateTime deadLetteredTimeUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a single record by its primary key, or null if none exists. Used by
    /// operator query paths and the replay function.
    /// </summary>
    Task<DeadLetterRecord?> GetByIdAsync(
        long deadLetterRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="maxRecords"/> records in
    /// <see cref="DeadLetterRecordStatus.PendingReplay"/> status, ordered by
    /// <c>StatusUpdatedUtc</c> ascending (oldest first). Intended for the replay
    /// function's scheduled batch.
    /// </summary>
    Task<IReadOnlyList<DeadLetterRecord>> GetPendingReplayBatchAsync(
        int maxRecords,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the lifecycle fields of a record. Intended for operator status flips
    /// and for the replay function recording an outcome. Only updates the fields
    /// listed in the parameters; other columns are left untouched.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no record exists with the given id.
    /// </exception>
    Task UpdateLifecycleAsync(
        long deadLetterRecordId,
        DeadLetterRecordStatus status,
        string? statusUpdatedBy,
        int? replayAttemptCount = null,
        DateTime? lastReplayAttemptUtc = null,
        string? lastReplayMessageId = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default);
}
