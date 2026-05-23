namespace FileIt.Domain.Entities.DeadLetter;

/// <summary>
/// Durable record of a Service Bus message that was dead-lettered. Written by a DLQ
/// reader function, read and updated by operators and the replay function.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors <c>dbo.DeadLetterRecord</c>. All constraints and indexes live on the SQL
/// side and are the source of truth; this class is the C# shape for reads and writes.
/// </para>
/// <para>
/// <b>Deliberately does NOT implement <c>IAuditable</c>.</b> The <c>IAuditable</c>
/// contract in this codebase was designed for low-volume audit log tables that use
/// <c>int</c> primary keys and local-time <c>CreatedOn</c>/<c>ModifiedOn</c>
/// timestamps. Dead-letter records are different on both counts:
/// <list type="bullet">
///   <item>
///     <description>
///       Primary key is <c>BIGINT</c> (<c>long</c> in C#) because a high-volume
///       event-driven system may accumulate well over the 2.1 billion row cap
///       of a 32-bit identity over its lifetime, and because this table is
///       append-dominant with a long retention window.
///     </description>
///   </item>
///   <item>
///     <description>
///       All timestamps are UTC (<c>CreatedUtc</c>, <c>DeadLetteredTimeUtc</c>,
///       <c>StatusUpdatedUtc</c>, <c>LastReplayAttemptUtc</c>). Dead-letter records
///       are correlated with <c>CommonLog</c> entries, Service Bus diagnostics, and
///       downstream system events that may originate in different timezones. UTC is
///       the only sane anchor for cross-system correlation.
///     </description>
///   </item>
/// </list>
/// Rather than weaken the schema to fit the legacy pattern, this entity stands on
/// its own. <see cref="FileIt.Domain.Interfaces.IDeadLetterRecordRepo"/> defines a
/// purpose-built contract instead of inheriting <c>IRepository&lt;T&gt;</c>.
/// </para>
/// <para>
/// Enum-typed properties (<see cref="SourceEntityType"/>, <see cref="FailureCategory"/>,
/// <see cref="DeadLetterRecordStatus"/>) are persisted as their string names via EF
/// value converters configured in <c>CommonDbContext.OnModelCreating</c>. This keeps
/// the C# type system and the database CHECK constraints in agreement and makes
/// illegal states unrepresentable in code.
/// </para>
/// <para>
/// See docs/dead-letter-strategy.md Section 6 for the full schema rationale.
/// </para>
/// </remarks>
public class DeadLetterRecord
{
    public long DeadLetterRecordId { get; set; }

    // Identity of the failing message ---------------------------------------------

    /// <summary>Service Bus MessageId of the dead-lettered message. Required.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Correlation ID used to join against CommonLog for end-to-end tracing.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Session ID if the message was session-enabled. Null otherwise.</summary>
    public string? SessionId { get; set; }

    // Source channel --------------------------------------------------------------

    /// <summary>Whether the source was a queue or a topic subscription.</summary>
    public SourceEntityType SourceEntityType { get; set; }

    /// <summary>Queue or topic name. Required.</summary>
    public string SourceEntityName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription name. Required when <see cref="SourceEntityType"/> is
    /// <see cref="SourceEntityType.Topic"/>, must be null when it is
    /// <see cref="SourceEntityType.Queue"/>. Enforced at the database via
    /// <c>CK_DeadLetterRecord_SubscriptionPresence</c>.
    /// </summary>
    public string? SourceSubscriptionName { get; set; }

    // Service Bus failure context -------------------------------------------------

    /// <summary>Service Bus <c>DeadLetterReason</c> application property.</summary>
    public string? DeadLetterReason { get; set; }

    /// <summary>Service Bus <c>DeadLetterErrorDescription</c> application property.</summary>
    public string? DeadLetterErrorDescription { get; set; }

    /// <summary>
    /// Number of delivery attempts before dead-lettering. Non-negative. Enforced at
    /// the database via <c>CK_DeadLetterRecord_DeliveryCount_NonNegative</c>.
    /// </summary>
    public int DeliveryCount { get; set; }

    /// <summary>When the message was originally enqueued, in UTC.</summary>
    public DateTime EnqueuedTimeUtc { get; set; }

    /// <summary>When the message was moved to the dead-letter sub-queue, in UTC.</summary>
    public DateTime DeadLetteredTimeUtc { get; set; }

    // FileIt classification -------------------------------------------------------

    /// <summary>Category assigned by the classifier. Never null in persisted rows.</summary>
    public FailureCategory FailureCategory { get; set; } = FailureCategory.Unknown;

    // Payload ---------------------------------------------------------------------

    /// <summary>
    /// Message body verbatim, as the reader received it. Required; without this
    /// column, replay is impossible.
    /// </summary>
    public string MessageBody { get; set; } = string.Empty;

    /// <summary>
    /// Application properties on the message envelope, serialized as JSON. Null if
    /// the message had no application properties.
    /// </summary>
    public string? MessageProperties { get; set; }

    /// <summary>Service Bus <c>ContentType</c> property, preserved for replay.</summary>
    public string? ContentType { get; set; }

    // Lifecycle -------------------------------------------------------------------

    /// <summary>Operator-facing status driving the replay workflow.</summary>
    public DeadLetterRecordStatus Status { get; set; } = DeadLetterRecordStatus.New;

    /// <summary>When <see cref="Status"/> was last updated, in UTC.</summary>
    public DateTime StatusUpdatedUtc { get; set; }

    /// <summary>Identity of the operator (or system) that last changed status.</summary>
    public string? StatusUpdatedBy { get; set; }

    // Replay telemetry ------------------------------------------------------------

    /// <summary>
    /// Count of replay attempts made by the replay function for this record.
    /// Non-negative. Enforced at the database via
    /// <c>CK_DeadLetterRecord_ReplayAttemptCount_NonNegative</c>.
    /// </summary>
    public int ReplayAttemptCount { get; set; }

    /// <summary>Timestamp of the most recent replay attempt, in UTC. Null if never replayed.</summary>
    public DateTime? LastReplayAttemptUtc { get; set; }

    /// <summary>
    /// Service Bus MessageId assigned to the re-published message on the most recent
    /// successful replay attempt. Null if never replayed or if all attempts failed.
    /// </summary>
    public string? LastReplayMessageId { get; set; }

    // Triage ----------------------------------------------------------------------

    /// <summary>
    /// Free-text notes. Initially populated with the classifier's reasoning string;
    /// operators may append triage notes during review.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    // Bookkeeping -----------------------------------------------------------------

    /// <summary>When the row was inserted, in UTC.</summary>
    public DateTime CreatedUtc { get; set; }
}
