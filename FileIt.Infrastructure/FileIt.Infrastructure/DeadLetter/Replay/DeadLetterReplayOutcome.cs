namespace FileIt.Infrastructure.DeadLetter.Replay;

/// <summary>
/// Outcome of replaying a single <see cref="FileIt.Domain.Entities.DeadLetter.DeadLetterRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// Encoded as a discriminated outcome rather than an exception because replay
/// failures are the normal case (the message was dead-lettered for a reason, replay
/// often hits the same failure). Returning structured outcomes lets the timer-driven
/// batch path keep processing the rest of the batch while still recording exactly
/// what happened to each record.
/// </para>
/// <para>
/// Persisted as logging context; the canonical durable record of a replay attempt is
/// the <c>DeadLetterRecord</c> row itself, updated via <see cref="FileIt.Domain.Interfaces.IDeadLetterRecordRepo.UpdateLifecycleAsync"/>.
/// </para>
/// </remarks>
/// <param name="DeadLetterRecordId">Id of the record this outcome describes.</param>
/// <param name="Result">High-level result code; see <see cref="DeadLetterReplayResult"/>.</param>
/// <param name="ReplayedMessageId">
/// Service Bus MessageId assigned to the re-published message when
/// <see cref="Result"/> is <see cref="DeadLetterReplayResult.Sent"/>; null otherwise.
/// </param>
/// <param name="Reason">
/// Human-readable explanation of the outcome. Always populated. Lands in the record's
/// <c>ResolutionNotes</c> column and in <c>CommonLog</c> for forensic queries.
/// </param>
public sealed record DeadLetterReplayOutcome(
    long DeadLetterRecordId,
    DeadLetterReplayResult Result,
    string? ReplayedMessageId,
    string Reason);

/// <summary>
/// High-level outcome categories for a replay attempt.
/// </summary>
public enum DeadLetterReplayResult
{
    /// <summary>
    /// Record was successfully re-published to its source channel. The record's
    /// status is now <see cref="FileIt.Domain.Entities.DeadLetter.DeadLetterRecordStatus.Replayed"/>.
    /// Whether the replayed message succeeds downstream is not known at this point.
    /// </summary>
    Sent = 0,

    /// <summary>
    /// No record exists with the given id. The caller likely supplied a stale id;
    /// no lifecycle update was made.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// Record exists but is not in <see cref="FileIt.Domain.Entities.DeadLetter.DeadLetterRecordStatus.PendingReplay"/>.
    /// Common cases: another replay invocation already moved it forward, or an
    /// operator discarded it between the caller's read and the replay attempt.
    /// No lifecycle update was made.
    /// </summary>
    NotEligible = 2,

    /// <summary>
    /// Record is structurally invalid for replay (e.g. missing source entity name,
    /// missing message body). The classifier should have prevented this; reaching
    /// here indicates an upstream defect or a hand-edited row. The record's status
    /// is set back to <see cref="FileIt.Domain.Entities.DeadLetter.DeadLetterRecordStatus.UnderReview"/>
    /// for operator attention.
    /// </summary>
    InvalidRecord = 3,

    /// <summary>
    /// Send failed at the broker. <see cref="DeadLetterReplayOutcome.Reason"/>
    /// contains the broker's error description. The record's status is left at
    /// <see cref="FileIt.Domain.Entities.DeadLetter.DeadLetterRecordStatus.PendingReplay"/>
    /// and <c>ReplayAttemptCount</c> is incremented; subsequent runs will retry.
    /// </summary>
    SendFailed = 4,
}