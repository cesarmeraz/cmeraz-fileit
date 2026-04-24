namespace FileIt.Domain.Entities.DeadLetter;

/// <summary>
/// Why a Service Bus message ended up in the dead-letter sub-queue.
/// Drives the operator's triage decision and defines which records a future
/// auto-replay engine would be allowed to touch.
/// </summary>
/// <remarks>
/// <para>
/// This is a domain concept, not an infrastructure concept. It describes the kind
/// of failure that occurred; the classifier (in FileIt.Infrastructure) is the
/// mechanism that assigns a category.
/// </para>
/// <para>
/// Persisted to <c>dbo.DeadLetterRecord.FailureCategory</c> as the string name via
/// an EF value converter. The database enforces the same set via
/// <c>CK_DeadLetterRecord_FailureCategory</c>. Do not rename or reorder without a
/// coordinated migration; the string names are the contract between C# and SQL.
/// </para>
/// <para>
/// See docs/dead-letter-strategy.md Section 4.
/// </para>
/// </remarks>
public enum FailureCategory
{
    /// <summary>
    /// Classifier could not confidently assign a category. Escalated for human review.
    /// Default only when no other rule matches; never a silent fallback.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A self-resolving blink: timeout, deadlock, 429, brief network partition.
    /// In practice most of these die in Tier 1 (in-process Polly retry) before ever
    /// reaching the DLQ. The ones that make it here are the tail of the distribution
    /// and are the only category a future auto-replay engine would be permitted to touch.
    /// </summary>
    Transient = 1,

    /// <summary>
    /// A sustained downstream outage, not a blink. Distinguishable from Transient by
    /// duration and scope: clusters of messages dead-lettered in a narrow time window
    /// with the same error signature. Operators replay in bulk after the dependency
    /// is confirmed recovered.
    /// </summary>
    DownstreamUnavailable = 2,

    /// <summary>
    /// The payload itself is wrong: missing required fields, wrong types, malformed CSV,
    /// unparseable JSON. Replaying without upstream correction will fail the same way
    /// every time. Triage action is to fix the publisher.
    /// </summary>
    SchemaViolation = 3,

    /// <summary>
    /// Payload is structurally valid but causes the handler to fail every time, typically
    /// because of a logic bug the input shape exposes. May also be a deliberate poison
    /// trigger used in testing. Do not replay without a code fix; file a bug.
    /// </summary>
    Poison = 4,
}
