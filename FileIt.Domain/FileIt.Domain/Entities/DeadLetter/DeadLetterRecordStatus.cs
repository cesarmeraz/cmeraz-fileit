namespace FileIt.Domain.Entities.DeadLetter;

/// <summary>
/// Lifecycle state of a <see cref="DeadLetterRecord"/>. Drives the operator workflow
/// and the replay function's eligibility filter.
/// </summary>
/// <remarks>
/// <para>
/// Persisted to <c>dbo.DeadLetterRecord.Status</c> as the string name via an EF value
/// converter. The database enforces the same set via <c>CK_DeadLetterRecord_Status</c>.
/// Do not rename or reorder without a coordinated migration.
/// </para>
/// <para>
/// State transitions:
/// <code>
///   New ----- operator reviews ----->  UnderReview
///    |                                      |
///    |  operator approves replay            |  operator decides to replay
///    |                                      |
///    v                                      v
///   PendingReplay  ---- replay function ---> Replayed
///    |                                      |
///    |  replay failed 3x                    |  replayed message succeeds downstream
///    |                                      |
///    v                                      v
///   UnderReview                           Resolved
///
///   New -------- operator discards --------> Discarded
///   UnderReview - operator discards --------> Discarded
/// </code>
/// </para>
/// </remarks>
public enum DeadLetterRecordStatus
{
    /// <summary>
    /// Default state when the reader persists the record. Awaits operator review.
    /// </summary>
    New = 0,

    /// <summary>
    /// Operator has looked at the record and is deciding what to do, or the replay
    /// function has escalated after repeated failed replay attempts.
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Operator has approved replay. The replay function's scheduled run will pick
    /// this row up on its next tick and re-publish the message to its source channel.
    /// </summary>
    PendingReplay = 2,

    /// <summary>
    /// Replay function successfully re-published the message. Downstream outcome
    /// unknown at this state; a successful replay that later dead-letters again will
    /// produce a new row.
    /// </summary>
    Replayed = 3,

    /// <summary>
    /// Replay succeeded and the message was processed successfully downstream.
    /// Terminal state.
    /// </summary>
    Resolved = 4,

    /// <summary>
    /// Operator decided not to replay (payload is unrecoverable, e.g. a poison trigger
    /// or schema violation that cannot be corrected). Terminal state.
    /// </summary>
    Discarded = 5,
}
